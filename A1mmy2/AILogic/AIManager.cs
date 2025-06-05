using Accord.Math.Distances;
using AILogic;
using A1mmy2.Class;
using Class;
using InputLogic;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Other;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Supercluster.KDTree;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Visuality;
using Device = SharpDX.Direct3D11.Device;

namespace A1mmy2.AILogic
{
    internal class AIManager : IDisposable
    {
        #region Variables

        private const int IMAGE_SIZE = 640;
        private const int NUM_DETECTIONS = 8400; // Standard for OnnxV8 model (Shape: 1x5x8400)
        private const int SAVE_FRAME_COOLDOWN_MS = 500;

        private DateTime lastSavedTime = DateTime.MinValue;
        private List<string>? _outputNames;
        private RectangleF LastDetectionBox;
        private KalmanPrediction kalmanPrediction;
        private WiseTheFoxPrediction wtfpredictionManager;

        private Bitmap? _screenCaptureBitmap;
        private byte[]? _bitmapBuffer; // Reusable buffer for bitmap operations

        // Screen capture
        private Device _dxDevice;
        private OutputDuplication _deskDuplication;
        private Texture2DDescription _texDesc;
        private Texture2D _stagingTex;

        private readonly int ScreenWidth = WinAPICaller.ScreenWidth;
        private readonly int ScreenHeight = WinAPICaller.ScreenHeight;

        private readonly RunOptions? _modeloptions;
        private InferenceSession? _onnxModel;

        private Thread? _aiLoopThread;
        private volatile bool _isAiLoopRunning;

        // For Auto-Labelling Data System
        private bool PlayerFound = false;

        private double CenterXTranslated = 0;
        private double CenterYTranslated = 0;

        // For Shall0e's Prediction Method
        private int PrevX = 0;
        private int PrevY = 0;

        private int IndependentMousePress = 0;

        private int detectedX { get; set; }
        private int detectedY { get; set; }

        public double AIConf = 0;
        private static int targetX, targetY;

        private Graphics? _graphics;

        // Pre-calculated values
        private readonly float _scaleX;
        private readonly float _scaleY;

        // Tensor reuse (model inference)
        private DenseTensor<float>? _reusableTensor;
        private float[]? _reusableInputArray;
        private List<NamedOnnxValue>? _reusableInputs;

        #endregion Variables

        public AIManager(string modelPath)
        {
            // Initialize DXGI capture once
            InitializeDxgiDuplication();

            // Pre-calculate scaling factors
            _scaleX = ScreenWidth / 640f;
            _scaleY = ScreenHeight / 640f;

            kalmanPrediction = new KalmanPrediction();
            wtfpredictionManager = new WiseTheFoxPrediction();

            _modeloptions = new RunOptions();

            var sessionOptions = new SessionOptions
            {
                EnableCpuMemArena = true,
                EnableMemoryPattern = true,
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                ExecutionMode = ExecutionMode.ORT_PARALLEL,
                InterOpNumThreads = Environment.ProcessorCount, // Auto adjust based on PC capabilities
                IntraOpNumThreads = Environment.ProcessorCount // Auto adjust based on PC capabilities
            };

            // Attempt to load via DirectML (else fallback to CPU)
            Task.Run(() => InitializeModel(sessionOptions, modelPath));
        }

        private void InitializeDxgiDuplication()
        {
            // Create D3D11 device
            _dxDevice = new Device(DriverType.Hardware, DeviceCreationFlags.None);

            // Grab the first output (primary monitor)
            using (var factory = new Factory1())
            using (var adapter = factory.Adapters1[0])
            {
                var output = adapter.Outputs[0];
                using (var output1 = output.QueryInterface<Output1>())
                {
                    _deskDuplication = output1.DuplicateOutput(_dxDevice);
                }
            }

            // Prepare a staging texture for CPU readback
            _texDesc = new Texture2DDescription
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Height = IMAGE_SIZE,
                Width = IMAGE_SIZE,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging
            };
            _stagingTex = new Texture2D(_dxDevice, _texDesc);
        }

        #region Models

        private async Task InitializeModel(SessionOptions sessionOptions, string modelPath)
        {
            try
            {
                await LoadModelAsync(sessionOptions, modelPath, useDirectML: true);
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    new NoticeBar($"Error starting the model via DirectML: {ex.Message}\n\nFalling back to CPU, performance may be poor.", 5000).Show()));
                try
                {
                    await LoadModelAsync(sessionOptions, modelPath, useDirectML: false);
                }
                catch (Exception e)
                {
                    await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        new NoticeBar($"Error starting the model via CPU: {e.Message}, you won't be able to aim assist at all.", 5000).Show()));
                }
            }

            FileManager.CurrentlyLoadingModel = false;
        }

        private async Task LoadModelAsync(SessionOptions sessionOptions, string modelPath, bool useDirectML)
        {
            try
            {
                if (useDirectML) { sessionOptions.AppendExecutionProvider_DML(); }
                else { sessionOptions.AppendExecutionProvider_CPU(); }

                _onnxModel = new InferenceSession(modelPath, sessionOptions);
                _outputNames = new List<string>(_onnxModel.OutputMetadata.Keys);

                // Validate the onnx model output shape (ensure model is OnnxV8)
                ValidateOnnxShape();

                // Pre-allocate bitmap buffer
                _bitmapBuffer = new byte[3 * IMAGE_SIZE * IMAGE_SIZE];
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    new NoticeBar($"Error starting the model: {ex.Message}", 5000).Show()));
                _onnxModel?.Dispose();
            }

            // Begin the loop
            _isAiLoopRunning = true;
            _aiLoopThread = new Thread(AiLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal // Higher priority for AI thread
            };
            _aiLoopThread.Start();
        }

        private void ValidateOnnxShape()
        {
            var expectedShape = new int[] { 1, 5, NUM_DETECTIONS };
            if (_onnxModel != null)
            {
                var outputMetadata = _onnxModel.OutputMetadata;
                if (!outputMetadata.Values.All(metadata => metadata.Dimensions.SequenceEqual(expectedShape)))
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    new NoticeBar(
                        $"Output shape does not match the expected shape of {string.Join("x", expectedShape)}.\n\nThis model will not work with A1mmy, please use an YOLOv8 model converted to ONNXv8."
                        , 15000)
                    .Show()
                    ));
                }
            }
        }

        #endregion Models

        #region AI

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldPredict() =>
            Dictionary.toggleState["Show Detected Player"] ||
            Dictionary.toggleState["Constant AI Tracking"] ||
            InputBindingManager.IsHoldingBinding("Aim Keybind") ||
            InputBindingManager.IsHoldingBinding("Second Aim Keybind");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ShouldProcess() =>
            Dictionary.toggleState["Aim Assist"] ||
            Dictionary.toggleState["Show Detected Player"] ||
            Dictionary.toggleState["Auto Trigger"];

        private async void AiLoop()
        {
            DetectedPlayerWindow? DetectedPlayerOverlay = Dictionary.DetectedPlayerOverlay;

            while (_isAiLoopRunning)
            {
                UpdateFOV();

                if (ShouldProcess())
                {
                    if (ShouldPredict())
                    {
                        Prediction? closestPrediction = await GetClosestPrediction();

                        if (closestPrediction == null)
                        {
                            DisableOverlay(DetectedPlayerOverlay!);
                            continue;
                        }

                        await AutoTrigger();
                        CalculateCoordinates(DetectedPlayerOverlay, closestPrediction, _scaleX, _scaleY);
                        HandleAim(closestPrediction);
                    }
                    else
                    {
                        // Processing so we are at the ready but not holding right/click.
                        await Task.Delay(1);
                    }
                }
                else
                {
                    // No work to do—sleep briefly to free up CPU
                    await Task.Delay(1);
                }
            }
        }

        #region AI Loop Functions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task AutoTrigger()
        {
            if (Dictionary.toggleState["Auto Trigger"] &&
                (InputBindingManager.IsHoldingBinding("Aim Keybind") || Dictionary.toggleState["Constant AI Tracking"]))
            {
                await MouseManager.DoTriggerClick();
                if (!Dictionary.toggleState["Aim Assist"] && !Dictionary.toggleState["Show Detected Player"]) return;
            }
        }

        private async void UpdateFOV()
        {
            if (Dictionary.dropdownState["Detection Area Type"] == "Closest to Mouse" && Dictionary.toggleState["FOV"])
            {
                var mousePosition = WinAPICaller.GetCursorPosition();
                await Application.Current.Dispatcher.BeginInvoke(() =>
                    Dictionary.FOVWindow.FOVStrictEnclosure.Margin = new Thickness(
                        Convert.ToInt16(mousePosition.X / WinAPICaller.scalingFactorX) - 320,
                        Convert.ToInt16(mousePosition.Y / WinAPICaller.scalingFactorY) - 320, 0, 0));
            }
        }

        private static void DisableOverlay(DetectedPlayerWindow DetectedPlayerOverlay)
        {
            if (Dictionary.toggleState["Show Detected Player"] && Dictionary.DetectedPlayerOverlay != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Dictionary.toggleState["Show AI Confidence"])
                    {
                        DetectedPlayerOverlay!.DetectedPlayerConfidence.Opacity = 0;
                    }

                    if (Dictionary.toggleState["Show Tracers"])
                    {
                        DetectedPlayerOverlay!.DetectedTracers.Opacity = 0;
                    }

                    DetectedPlayerOverlay!.DetectedPlayerFocus.Opacity = 0;
                });
            }
        }

        private void UpdateOverlay(DetectedPlayerWindow DetectedPlayerOverlay)
        {
            var scalingFactorX = WinAPICaller.scalingFactorX;
            var scalingFactorY = WinAPICaller.scalingFactorY;
            var centerX = Convert.ToInt16(LastDetectionBox.X / scalingFactorX) + (LastDetectionBox.Width / 2.0);
            var centerY = Convert.ToInt16(LastDetectionBox.Y / scalingFactorY);

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Dictionary.toggleState["Show AI Confidence"])
                {
                    DetectedPlayerOverlay.DetectedPlayerConfidence.Opacity = 1;
                    DetectedPlayerOverlay.DetectedPlayerConfidence.Content = $"{Math.Round((AIConf * 100), 2)}%";

                    var labelEstimatedHalfWidth = DetectedPlayerOverlay.DetectedPlayerConfidence.ActualWidth / 2.0;
                    DetectedPlayerOverlay.DetectedPlayerConfidence.Margin = new Thickness(
                        centerX - labelEstimatedHalfWidth,
                        centerY - DetectedPlayerOverlay.DetectedPlayerConfidence.ActualHeight - 2, 0, 0);
                }

                var showTracers = Dictionary.toggleState["Show Tracers"];
                DetectedPlayerOverlay.DetectedTracers.Opacity = showTracers ? 1 : 0;
                if (showTracers)
                {
                    DetectedPlayerOverlay.DetectedTracers.X2 = centerX;
                    DetectedPlayerOverlay.DetectedTracers.Y2 = centerY + LastDetectionBox.Height;
                }

                DetectedPlayerOverlay.Opacity = Dictionary.sliderSettings["Opacity"];

                DetectedPlayerOverlay.DetectedPlayerFocus.Opacity = 1;
                DetectedPlayerOverlay.DetectedPlayerFocus.Margin = new Thickness(
                    centerX - (LastDetectionBox.Width / 2.0), centerY, 0, 0);
                DetectedPlayerOverlay.DetectedPlayerFocus.Width = LastDetectionBox.Width;
                DetectedPlayerOverlay.DetectedPlayerFocus.Height = LastDetectionBox.Height;
            });
        }

        private void CalculateCoordinates(DetectedPlayerWindow DetectedPlayerOverlay, Prediction closestPrediction, float scaleX, float scaleY)
        {
            AIConf = closestPrediction.Confidence;

            if (Dictionary.toggleState["Show Detected Player"] && Dictionary.DetectedPlayerOverlay != null)
            {
                UpdateOverlay(DetectedPlayerOverlay!);
                if (!Dictionary.toggleState["Aim Assist"]) return;
            }

            double YOffset = Dictionary.sliderSettings["Y Offset (Up/Down)"];
            double XOffset = Dictionary.sliderSettings["X Offset (Left/Right)"];

            double YOffsetPercentage = Dictionary.sliderSettings["Y Offset (%)"];
            double XOffsetPercentage = Dictionary.sliderSettings["X Offset (%)"];

            var rect = closestPrediction.Rectangle;

            if (Dictionary.toggleState["X Axis Percentage Adjustment"])
            {
                detectedX = (int)((rect.X + (rect.Width * (XOffsetPercentage / 100))) * scaleX);
            }
            else
            {
                detectedX = (int)((rect.X + rect.Width / 2) * scaleX + XOffset);
            }

            if (Dictionary.toggleState["Y Axis Percentage Adjustment"])
            {
                detectedY = (int)((rect.Y + rect.Height - (rect.Height * (YOffsetPercentage / 100))) * scaleY + YOffset);
            }
            else
            {
                detectedY = CalculateDetectedY(scaleY, YOffset, closestPrediction);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalculateDetectedY(float scaleY, double YOffset, Prediction closestPrediction)
        {
            var rect = closestPrediction.Rectangle;
            float yBase = rect.Y;
            float yAdjustment = 0;

            switch (Dictionary.dropdownState["Aiming Boundaries Alignment"])
            {
                case "Center":
                    yAdjustment = rect.Height / 2;
                    break;

                case "Top":
                    // yBase is already at the top
                    break;

                case "Bottom":
                    yAdjustment = rect.Height;
                    break;
            }

            return (int)((yBase + yAdjustment) * scaleY + YOffset);
        }

        private void HandleAim(Prediction closestPrediction)
        {
            if (Dictionary.toggleState["Aim Assist"] &&
                (Dictionary.toggleState["Constant AI Tracking"] ||
                 Dictionary.toggleState["Aim Assist"] && InputBindingManager.IsHoldingBinding("Aim Keybind") ||
                 Dictionary.toggleState["Aim Assist"] && InputBindingManager.IsHoldingBinding("Second Aim Keybind")))
            {
                if (Dictionary.toggleState["Predictions"])
                {
                    HandlePredictions(kalmanPrediction, closestPrediction, detectedX, detectedY);
                }
                else
                {
                    MouseManager.MoveCrosshair(detectedX, detectedY);
                }
            }
        }

        private void HandlePredictions(KalmanPrediction kalmanPrediction, Prediction closestPrediction, int detectedX, int detectedY)
        {
            var predictionMethod = Dictionary.dropdownState["Prediction Method"];
            switch (predictionMethod)
            {
                case "Kalman Filter":
                    KalmanPrediction.Detection detection = new()
                    {
                        X = detectedX,
                        Y = detectedY,
                        Timestamp = DateTime.UtcNow
                    };

                    kalmanPrediction.UpdateKalmanFilter(detection);
                    var predictedPosition = kalmanPrediction.GetKalmanPosition();

                    MouseManager.MoveCrosshair(predictedPosition.X, predictedPosition.Y);
                    break;

                case "Shall0e's Prediction":
                    ShalloePredictionV2.xValues.Add(detectedX - PrevX);
                    ShalloePredictionV2.yValues.Add(detectedY - PrevY);

                    // Optimize: only keep last 5 values
                    if (ShalloePredictionV2.xValues.Count > 5)
                    {
                        ShalloePredictionV2.xValues.RemoveAt(0);
                        ShalloePredictionV2.yValues.RemoveAt(0);
                    }

                    MouseManager.MoveCrosshair(ShalloePredictionV2.GetSPX(), detectedY);

                    PrevX = detectedX;
                    PrevY = detectedY;
                    break;

                case "wisethef0x's EMA Prediction":
                    WiseTheFoxPrediction.WTFDetection wtfdetection = new()
                    {
                        X = detectedX,
                        Y = detectedY,
                        Timestamp = DateTime.UtcNow
                    };

                    wtfpredictionManager.UpdateDetection(wtfdetection);
                    var wtfpredictedPosition = wtfpredictionManager.GetEstimatedPosition();

                    MouseManager.MoveCrosshair(wtfpredictedPosition.X, detectedY);
                    break;
            }
        }

        private async Task<Prediction?> GetClosestPrediction(bool useMousePosition = true)
        {
            targetX = Dictionary.dropdownState["Detection Area Type"] == "Closest to Mouse" ?
                WinAPICaller.GetCursorPosition().X : ScreenWidth / 2;
            targetY = Dictionary.dropdownState["Detection Area Type"] == "Closest to Mouse" ?
                WinAPICaller.GetCursorPosition().Y : ScreenHeight / 2;

            Rectangle detectionBox = new(targetX - IMAGE_SIZE / 2, targetY - IMAGE_SIZE / 2, IMAGE_SIZE, IMAGE_SIZE);

            Bitmap? frame = ScreenGrab(detectionBox);
            if (frame == null) return null;

            float[] inputArray;
            if (_reusableInputArray == null || _reusableInputArray.Length != 3 * IMAGE_SIZE * IMAGE_SIZE)
            {
                _reusableInputArray = new float[3 * IMAGE_SIZE * IMAGE_SIZE];
            }
            inputArray = _reusableInputArray;

            // Fill the reusable array
            BitmapToFloatArrayInPlace(frame, inputArray);

            // Reuse tensor and inputs
            if (_reusableTensor == null)
            {
                _reusableTensor = new DenseTensor<float>(inputArray, new int[] { 1, 3, IMAGE_SIZE, IMAGE_SIZE });
                _reusableInputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("images", _reusableTensor) };
            }
            else
            {
                // Directly copy into existing DenseTensor buffer
                inputArray.AsSpan().CopyTo(_reusableTensor.Buffer.Span);
            }

            if (_onnxModel == null) return null;

            var results = _onnxModel.Run(_reusableInputs, _outputNames, _modeloptions);
            var outputTensor = results[0].AsTensor<float>();

            // Calculate the FOV boundaries
            float FovSize = (float)Dictionary.sliderSettings["FOV Size"];
            float fovMinX = (IMAGE_SIZE - FovSize) / 2.0f;
            float fovMaxX = (IMAGE_SIZE + FovSize) / 2.0f;
            float fovMinY = (IMAGE_SIZE - FovSize) / 2.0f;
            float fovMaxY = (IMAGE_SIZE + FovSize) / 2.0f;

            (List<double[]> KDpoints, List<Prediction> KDPredictions) = PrepareKDTreeData(outputTensor, detectionBox, fovMinX, fovMaxX, fovMinY, fovMaxY);

            if (KDpoints.Count == 0 || KDPredictions.Count == 0)
            {
                return null;
            }

            var tree = new KDTree<double, Prediction>(2, KDpoints.ToArray(), KDPredictions.ToArray(), L2Norm_Squared_Double);
            var nearest = tree.NearestNeighbors(new double[] { IMAGE_SIZE / 2.0, IMAGE_SIZE / 2.0 }, 1);

            if (nearest != null && nearest.Length > 0)
            {
                // Translate coordinates
                float translatedXMin = nearest[0].Item2.Rectangle.X + detectionBox.Left;
                float translatedYMin = nearest[0].Item2.Rectangle.Y + detectionBox.Top;
                LastDetectionBox = new RectangleF(translatedXMin, translatedYMin,
                    nearest[0].Item2.Rectangle.Width, nearest[0].Item2.Rectangle.Height);

                CenterXTranslated = nearest[0].Item2.CenterXTranslated;
                CenterYTranslated = nearest[0].Item2.CenterYTranslated;

                SaveFrame(frame, nearest[0].Item2);
                return nearest[0].Item2;
            }
            else if (Dictionary.toggleState["Collect Data While Playing"] &&
                     !Dictionary.toggleState["Constant AI Tracking"] &&
                     !Dictionary.toggleState["Auto Label Data"])
            {
                SaveFrame(frame);
            }

            return null;
        }

        private (List<double[]>, List<Prediction>) PrepareKDTreeData(Tensor<float> outputTensor, Rectangle detectionBox,
            float fovMinX, float fovMaxX, float fovMinY, float fovMaxY)
        {
            float minConfidence = (float)Dictionary.sliderSettings["AI Minimum Confidence"] / 100.0f;

            var KDpoints = new List<double[]>(100); // Pre-allocate with estimated capacity
            var KDpredictions = new List<Prediction>(100);

            for (int i = 0; i < NUM_DETECTIONS; i++)
            {
                float objectness = outputTensor[0, 4, i];
                if (objectness < minConfidence) continue;

                float x_center = outputTensor[0, 0, i];
                float y_center = outputTensor[0, 1, i];
                float width = outputTensor[0, 2, i];
                float height = outputTensor[0, 3, i];

                float x_min = x_center - width / 2;
                float y_min = y_center - height / 2;
                float x_max = x_center + width / 2;
                float y_max = y_center + height / 2;

                if (x_min < fovMinX || x_max > fovMaxX || y_min < fovMinY || y_max > fovMaxY) continue;

                RectangleF rect = new(x_min, y_min, width, height);
                Prediction prediction = new()
                {
                    Rectangle = rect,
                    Confidence = objectness,
                    CenterXTranslated = (x_center - detectionBox.Left) / IMAGE_SIZE,
                    CenterYTranslated = (y_center - detectionBox.Top) / IMAGE_SIZE
                };

                KDpoints.Add(new double[] { x_center, y_center });
                KDpredictions.Add(prediction);
            }

            return (KDpoints, KDpredictions);
        }

        #endregion AI Loop Functions

        #endregion AI

        #region Screen Capture

        private void SaveFrame(Bitmap frame, Prediction? DoLabel = null)
        {
            if (!Dictionary.toggleState["Collect Data While Playing"] && Dictionary.toggleState["Constant AI Tracking"]) return;
            if ((DateTime.Now - lastSavedTime).TotalMilliseconds < SAVE_FRAME_COOLDOWN_MS) return;

            lastSavedTime = DateTime.Now;
            string uuid = Guid.NewGuid().ToString();

            // Clone the bitmap to avoid threading issues
            string imagePath = Path.Combine("bin", "images", $"{uuid}.jpg");

            // Save synchronously to avoid "Object is currently in use elsewhere" error
            frame.Save(imagePath, ImageFormat.Jpeg);

            if (Dictionary.toggleState["Auto Label Data"] && DoLabel != null)
            {
                var labelPath = Path.Combine("bin", "labels", $"{uuid}.txt");

                float x = (DoLabel!.Rectangle.X + DoLabel.Rectangle.Width / 2) / frame.Width;
                float y = (DoLabel!.Rectangle.Y + DoLabel.Rectangle.Height / 2) / frame.Height;
                float width = DoLabel.Rectangle.Width / frame.Width;
                float height = DoLabel.Rectangle.Height / frame.Height;

                File.WriteAllText(labelPath, $"0 {x} {y} {width} {height}");
            }
        }

        public Bitmap? ScreenGrab(Rectangle detectionBox)
        {
            // 1) Clamp detectionBox so it never goes off-screen
            if (detectionBox.Left < 0) detectionBox.X = 0;
            if (detectionBox.Top < 0) detectionBox.Y = 0;
            if (detectionBox.Right > ScreenWidth) detectionBox.X = ScreenWidth - IMAGE_SIZE;
            if (detectionBox.Bottom > ScreenHeight) detectionBox.Y = ScreenHeight - IMAGE_SIZE;

            // 2) Acquire the next frame from desktop duplication
            SharpDX.DXGI.Resource? desktopResource = null;
            OutputDuplicateFrameInformation frameInfo;
            Result result;

            try
            {
                result = _deskDuplication.TryAcquireNextFrame(0, out frameInfo, out desktopResource);
            }
            catch (SharpDXException)
            {
                // If duplication is lost, attempt to recreate it and return null this frame
                ReinitializeDxgiDuplication();
                return null;
            }

            // If TryAcquireNextFrame failed or desktopResource is null, bail out
            if (result.Failure || desktopResource == null)
            {
                if (desktopResource != null)
                {
                    desktopResource.Dispose();
                    desktopResource = null;
                }
                return null;
            }

            try
            {
                // Now we have a valid desktopResource
                using (desktopResource)
                {
                    using (var screenTexture2D = desktopResource.QueryInterface<Texture2D>())
                    {
                        // Copy the region of interest to staging texture
                        var region = new ResourceRegion
                        {
                            Left = detectionBox.Left,
                            Top = detectionBox.Top,
                            Right = detectionBox.Left + detectionBox.Width,
                            Bottom = detectionBox.Top + detectionBox.Height,
                            Front = 0,
                            Back = 1
                        };

                        _dxDevice.ImmediateContext.CopySubresourceRegion(
                            screenTexture2D, 0, region, _stagingTex, 0, 0, 0, 0);

                        // Map staging texture to CPU, then build a Bitmap
                        var dataBox = _dxDevice.ImmediateContext.MapSubresource(
                            _stagingTex, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

                        try
                        {
                            // If bitmap not allocated or wrong size, re-create
                            if (_screenCaptureBitmap == null
                                || _screenCaptureBitmap.Width != detectionBox.Width
                                || _screenCaptureBitmap.Height != detectionBox.Height)
                            {
                                _screenCaptureBitmap?.Dispose();
                                _screenCaptureBitmap = new Bitmap(detectionBox.Width, detectionBox.Height, PixelFormat.Format32bppArgb);
                            }

                            // Copy from staging data to Bitmap
                            var bmpData = _screenCaptureBitmap.LockBits(
                                new Rectangle(0, 0, detectionBox.Width, detectionBox.Height),
                                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

                            Utilities.CopyMemory(
                                bmpData.Scan0,
                                dataBox.DataPointer,
                                detectionBox.Width * detectionBox.Height * 4);

                            _screenCaptureBitmap.UnlockBits(bmpData);
                        }
                        finally
                        {
                            _dxDevice.ImmediateContext.UnmapSubresource(_stagingTex, 0);
                        }
                    }
                }

                // Release this frame so the next one can be acquired
                _deskDuplication.ReleaseFrame();
                return _screenCaptureBitmap;
            }
            catch (SharpDXException)
            {
                // If for some reason the copy or map fails, attempt to reinitialize duplication
                ReinitializeDxgiDuplication();
                return null;
            }
        }

        private void ReinitializeDxgiDuplication()
        {
            // Clean up any existing duplication objects
            try { _deskDuplication?.Dispose(); } catch { }
            try { _stagingTex?.Dispose(); } catch { }

            // Recreate the duplication chain
            _dxDevice?.Dispose();
            InitializeDxgiDuplication();
        }

        #endregion Screen Capture

        #region Optimized Math

        public static Func<double[], double[], double> L2Norm_Squared_Double = (x, y) =>
        {
            double dist = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                dist += (x[i] - y[i]) * (x[i] - y[i]);
            }

            return dist;
        };

        private unsafe void BitmapToFloatArrayInPlace(Bitmap image, float[] result)
        {
            int height = image.Height;
            int width = image.Width;
            const float multiplier = 1f / 255f;

            // Lock the bits once
            var rect = new Rectangle(0, 0, width, height);
            var bmpData = image.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            try
            {
                int stride = bmpData.Stride;
                int totalPixels = width * height;
                int redOffset = 0;
                int greenOffset = totalPixels;
                int blueOffset = 2 * totalPixels;

                byte* basePtr = (byte*)bmpData.Scan0.ToPointer();
                // For each channel, copy entire component block
                // We know format is 24bpp BGR so each pixel is [B][G][R] per byte

                // Create temporary buffer to hold one row of bytes
                byte[] rowBuffer = new byte[width * 3];

                for (int y = 0; y < height; y++)
                {
                    byte* rowPtr = basePtr + y * stride;
                    // Copy this entire row: width*3 bytes
                    Marshal.Copy((IntPtr)rowPtr, rowBuffer, 0, rowBuffer.Length);

                    // Now extract channels
                    int pixelIndexBase = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        int bufferIndex = x * 3;
                        // BGR byte order: rowBuffer[bufferIndex + 2] = R, +1 = G, +0 = B
                        result[redOffset + pixelIndexBase + x] = rowBuffer[bufferIndex + 2] * multiplier;
                        result[greenOffset + pixelIndexBase + x] = rowBuffer[bufferIndex + 1] * multiplier;
                        result[blueOffset + pixelIndexBase + x] = rowBuffer[bufferIndex] * multiplier;
                    }
                }
            }
            finally
            {
                image.UnlockBits(bmpData);
            }
        }

        #endregion Optimized Math

        public void Dispose()
        {
            // Stop the loop
            _isAiLoopRunning = false;
            if (_aiLoopThread != null && _aiLoopThread.IsAlive)
            {
                if (!_aiLoopThread.Join(TimeSpan.FromSeconds(1)))
                {
                    try
                    {
                        _aiLoopThread.Interrupt();
                    }
                    catch { }
                }
            }

            // Dispose DXGI objects
            _stagingTex?.Dispose();
            _deskDuplication?.Dispose();
            _dxDevice?.Dispose();

            // Clean up reusable resources
            _reusableInputArray = null;
            _reusableInputs = null;

            _onnxModel?.Dispose();
            _modeloptions?.Dispose();
            _bitmapBuffer = null;
        }

        public class Prediction
        {
            public RectangleF Rectangle { get; set; }
            public float Confidence { get; set; }
            public float CenterXTranslated { get; set; }
            public float CenterYTranslated { get; set; }
        }
    }
}