using Aimmy2.Class;
using Class;
using InputLogic;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.Win32;
using Other;
using SharpGen.Runtime;
using Supercluster.KDTree;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using Visuality;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using System.Runtime.InteropServices;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using ComputeSharp;

namespace Aimmy2.AILogic
{
    internal class AIManager : IDisposable
    {
        #region Variables

        private const int IMAGE_SIZE = 640;
        private const int NUM_DETECTIONS = 8400; // Standard for OnnxV8 model (Shape: 1x5x8400)

        private DateTime lastSavedTime = DateTime.MinValue;
        private List<string>? _outputNames;
        private RectangleF LastDetectionBox;

        private Bitmap? _screenCaptureBitmap;

        private readonly int ScreenWidth = WinAPICaller.ScreenWidth;
        private readonly int ScreenHeight = WinAPICaller.ScreenHeight;

        private readonly RunOptions? _modeloptions;
        private InferenceSession? _onnxModel;

        private Thread? _aiLoopThread;
        private bool _isAiLoopRunning;

        //Direct3D Variables
        private ID3D11Device _device;
        private ID3D11DeviceContext _context;
        private IDXGIOutputDuplication _outputDuplication;
        private ID3D11Texture2D _desktopImage;
        private Bitmap? _captureBitmap;


        // For Auto-Labelling Data System
        private bool PlayerFound = false;

        private double CenterXTranslated = 0;
        private double CenterYTranslated = 0;

        public static bool TPS = false;

        // For Shall0e's Prediction Method
        private int PrevX = 0;

        private int PrevY = 0;

        private int IndependentMousePress = 0;

        private int iterationCount = 0;
        private long totalTime = 0;

        private int detectedX { get; set; }
        private int detectedY { get; set; }

        public double AIConf = 0;
        private static int targetX, targetY;

        private Graphics? _graphics;

        #endregion Variables

        public AIManager(string modelPath)
        {

            _modeloptions = new RunOptions();

            var sessionOptions = new SessionOptions
            {
                EnableCpuMemArena = true,
                EnableMemoryPattern = true,
                GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                ExecutionMode = ExecutionMode.ORT_PARALLEL
            };
            SystemEvents.DisplaySettingsChanged += (s, e) =>
            {
                ReinitializeD3D11();
            };

            // Attempt to load via DirectML (else fallback to CPU)
            Task.Run(() => InitializeModel(sessionOptions, modelPath));
            InitializeDirectX();
        }
        #region DirectX
        private void InitializeDirectX()
        {
            try
            {
                DisposeD311();

                // Initialize Direct3D11 device and context
                FeatureLevel[] featureLevels = new[]
                   {
                        FeatureLevel.Level_12_1,
                        FeatureLevel.Level_12_0,
                        FeatureLevel.Level_11_1,
                        FeatureLevel.Level_11_0,
                        FeatureLevel.Level_10_1,
                        FeatureLevel.Level_10_0,
                        FeatureLevel.Level_9_3,
                        FeatureLevel.Level_9_2,
                        FeatureLevel.Level_9_1
                    };
                var result = D3D11.D3D11CreateDevice(
                    null,
                    DriverType.Hardware,
                    DeviceCreationFlags.BgraSupport,
                    featureLevels,
                    out _device,
                    out FeatureLevel featureLevel, // DEBUG
                    out _context
                );
                //FileManager.LogInfo($"Direct3D11 Feature Level Selected: {featureLevel}");
                if (result != Result.Ok || _device == null || _context == null)
                {
                    throw new InvalidOperationException($"Failed to create Direct3D11 device or context. HRESULT: {result}");
                }

                using var dxgiDevice = _device.QueryInterface<IDXGIDevice>();
                using var adapterForOutput = dxgiDevice.GetAdapter();
                var resultEnum = adapterForOutput.EnumOutputs(0, out var outputTemp);
                if (resultEnum != Result.Ok || outputTemp == null)
                {
                    throw new InvalidOperationException("Failed to enumerate outputs.");
                }


                using var output = outputTemp.QueryInterface<IDXGIOutput1>() ?? throw new InvalidOperationException("Failed to acquire IDXGIOutput1.");

                // Duplicate the output
                _outputDuplication = output.DuplicateOutput(_device);

                //FileManager.LogInfo("Direct3D11 device, context, and output duplication initialized.");
            }
            catch (Exception ex)
            {
                //FileManager.LogError("Error initializing Direct3D11: " + ex);
            }
        }

        #endregion
        #region Models

        private async Task InitializeModel(SessionOptions sessionOptions, string modelPath)
        {
            try
            {
                await LoadModelAsync(sessionOptions, modelPath, useDirectML: true);
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.BeginInvoke(new Action(() => new NoticeBar($"Error starting the model via DirectML: {ex.Message}\n\nFalling back to CPU, performance may be poor.", 5000).Show()));
                try
                {
                    await LoadModelAsync(sessionOptions, modelPath, useDirectML: false);
                }
                catch (Exception e)
                {
                    await Application.Current.Dispatcher.BeginInvoke(new Action(() => new NoticeBar($"Error starting the model via CPU: {e.Message}, you won't be able to aim assist at all.", 5000).Show()));
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
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.BeginInvoke(new Action(() => new NoticeBar($"Error starting the model: {ex.Message}", 5000).Show()));
                _onnxModel?.Dispose();
            }

            // Begin the loop
            _isAiLoopRunning = true;
            _aiLoopThread = new Thread(AiLoop);
            _aiLoopThread.IsBackground = true;
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
                        $"Output shape does not match the expected shape of {string.Join("x", expectedShape)}.\n\nThis model will not work with Aimmy, please use an YOLOv8 model converted to ONNXv8."
                        , 15000)
                    .Show()
                    ));
                }
            }
        }

        #endregion Models
        #region AI

        private static bool ShouldPredict() => Dictionary.toggleState["Show Detected Player"] || Dictionary.toggleState["Constant AI Tracking"] || InputBindingManager.IsHoldingBinding("Aim Keybind") || InputBindingManager.IsHoldingBinding("Second Aim Keybind");
        private static bool ShouldProcess() => Dictionary.toggleState["Aim Assist"] || Dictionary.toggleState["Show Detected Player"] || Dictionary.toggleState["Auto Trigger"];


        private async void AiLoop()
        {
            Stopwatch stopwatch = new();
            DetectedPlayerWindow? DetectedPlayerOverlay = Dictionary.DetectedPlayerOverlay;
            float scaleX = ScreenWidth / 640f, scaleY = ScreenHeight / 640f;
            stopwatch.Start();
            while (_isAiLoopRunning)
            {
                stopwatch.Restart();
                UpdateFOV();
                if (iterationCount == 1000)
                {
                    if (Dictionary.toggleState["Debug Mode"])
                        Application.Current.Dispatcher.Invoke(() => new NoticeBar($"Average AI Loop Time: {totalTime / 1000.0}ms", 5000).Show());
                    totalTime = 0;
                    iterationCount = 0;
                }
                if (ShouldProcess() && ShouldPredict())
                {
                    var closestPrediction = await GetClosestPrediction();
                    if (closestPrediction == null)
                    {
                        DisableOverlay(DetectedPlayerOverlay!);
                        continue;
                    }
                    await AutoTrigger();
                    CalculateCoordinates(DetectedPlayerOverlay, closestPrediction, scaleX, scaleY);
                    HandleAim(closestPrediction);
                    totalTime += stopwatch.ElapsedMilliseconds;
                    iterationCount++;
                }
                await Task.Delay(1);
            }
            stopwatch.Stop();
        }


        #endregion
        #region AI Loop Functions
        #region misc
        private async Task AutoTrigger()
        {
            if (Dictionary.toggleState["Auto Trigger"] &&
                (InputBindingManager.IsHoldingBinding("Aim Keybind") ||
                 InputBindingManager.IsHoldingBinding("Second Aim Keybind") ||
                 Dictionary.toggleState["Constant AI Tracking"]))
            {
                await MouseManager.DoTriggerClick();
                if (!Dictionary.toggleState["Aim Assist"] && !Dictionary.toggleState["Show Detected Player"])
                {
                    return;
                }
            }
        }

        private async void UpdateFOV()
        {
            if (Dictionary.dropdownState["Detection Area Type"] == "Closest to Mouse" && Dictionary.toggleState["FOV"])
            {
                var mousePosition = WinAPICaller.GetCursorPosition();
                if (Dictionary.FOVWindow != null) await Application.Current.Dispatcher.BeginInvoke(() => Dictionary.FOVWindow.FOVStrictEnclosure.Margin = new Thickness(Convert.ToInt16(mousePosition.X / WinAPICaller.scalingFactorX) - 320, Convert.ToInt16(mousePosition.Y / WinAPICaller.scalingFactorY) - 320, 0, 0));
            }
        }
        #endregion
        #region ESP
        private static void DisableOverlay(DetectedPlayerWindow DetectedPlayerOverlay)
        {
            if (Dictionary.toggleState["Show Detected Player"] && Dictionary.DetectedPlayerOverlay != null)
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
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
                }));
            }
        }

        private void UpdateOverlay(DetectedPlayerWindow detectedPlayerOverlay)
        {
            double scalingFactorX = WinAPICaller.scalingFactorX;
            double scalingFactorY = WinAPICaller.scalingFactorY;

            double centerX = LastDetectionBox.X / scalingFactorX + (LastDetectionBox.Width / 2.0);
            double centerY = LastDetectionBox.Y / scalingFactorY;
            double boxWidth = LastDetectionBox.Width;
            double boxHeight = LastDetectionBox.Height;

            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateConfidence(detectedPlayerOverlay, centerX, centerY);
                UpdateTracers(detectedPlayerOverlay, centerX, centerY, boxHeight);
                UpdateFocusBox(detectedPlayerOverlay, centerX, centerY, boxWidth, boxHeight);
            });
        }
        private void UpdateConfidence(DetectedPlayerWindow detectedPlayerOverlay, double centerX, double centerY)
        {
            if (Dictionary.toggleState["Show AI Confidence"])
            {
                detectedPlayerOverlay.DetectedPlayerConfidence.Opacity = 1;
                detectedPlayerOverlay.DetectedPlayerConfidence.Content = $"{Math.Round((AIConf * 100), 2)}%";

                double labelEstimatedHalfWidth = detectedPlayerOverlay.DetectedPlayerConfidence.ActualWidth / 2.0;
                detectedPlayerOverlay.DetectedPlayerConfidence.Margin = new Thickness(centerX - labelEstimatedHalfWidth, centerY - detectedPlayerOverlay.DetectedPlayerConfidence.ActualHeight - 2, 0, 0);
            }
            else
            {
                detectedPlayerOverlay.DetectedPlayerConfidence.Opacity = 0;
            }
        }

        private void UpdateTracers(DetectedPlayerWindow detectedPlayerOverlay, double centerX, double centerY, double boxHeight)
        {
            bool showTracers = Dictionary.toggleState["Show Tracers"];
            detectedPlayerOverlay.DetectedTracers.Opacity = showTracers ? 1 : 0;

            if (showTracers)
            {
                detectedPlayerOverlay.DetectedTracers.X2 = centerX;
                detectedPlayerOverlay.DetectedTracers.Y2 = centerY + boxHeight;
            }
        }

        private void UpdateFocusBox(DetectedPlayerWindow detectedPlayerOverlay, double centerX, double centerY, double boxWidth, double boxHeight)
        {
            detectedPlayerOverlay.DetectedPlayerFocus.Opacity = 1;
            detectedPlayerOverlay.DetectedPlayerFocus.Margin = new Thickness(centerX - (boxWidth / 2.0), centerY, 0, 0);
            detectedPlayerOverlay.DetectedPlayerFocus.Width = boxWidth;
            detectedPlayerOverlay.DetectedPlayerFocus.Height = boxHeight;

            detectedPlayerOverlay.Opacity = Dictionary.sliderSettings["Opacity"];
        }
        #endregion
        #region Coordinates
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
            double rectX = rect.X;
            double rectY = rect.Y;
            double rectWidth = rect.Width;
            double rectHeight = rect.Height;

            if (Dictionary.toggleState["X Axis Percentage Adjustment"])
            {
                detectedX = (int)((rectX + (rectWidth * (XOffsetPercentage / 100))) * scaleX);
            }
            else
            {
                detectedX = (int)((rectX + rectWidth / 2) * scaleX + XOffset);
            }

            if (Dictionary.toggleState["Y Axis Percentage Adjustment"])
            {
                detectedY = (int)((rectY + rectHeight - (rectHeight * (YOffsetPercentage / 100))) * scaleY + YOffset);
            }
            else
            {
                detectedY = CalculateDetectedY(scaleY, YOffset, closestPrediction);
            }
        }
        private static int CalculateDetectedY(float scaleY, double YOffset, Prediction closestPrediction)
        {
            var rect = closestPrediction.Rectangle;
            float yBase = rect.Y;
            float yAdjustment = Dictionary.dropdownState["Aiming Boundaries Alignment"] switch
            {
                "Center" => rect.Height / 2,
                "Bottom" => rect.Height,
                _ => 0 // Default case for "Top" and any other unexpected values
            };

            return (int)((yBase + yAdjustment) * scaleY + YOffset);
        }
        #endregion
        #region Mouse Movement
        private void HandleAim(Prediction closestPrediction)
        {
            if (Dictionary.toggleState["Aim Assist"] && (Dictionary.toggleState["Constant AI Tracking"]
                || Dictionary.toggleState["Aim Assist"] && InputBindingManager.IsHoldingBinding("Aim Keybind")
                || Dictionary.toggleState["Aim Assist"] && InputBindingManager.IsHoldingBinding("Second Aim Keybind")))
            {
                MouseManager.MoveCrosshair(detectedX, detectedY);
            }
        }
        #endregion
        #region Prediction (AI Work)
        private Rectangle ClampRectangle(Rectangle rect, int screenWidth, int screenHeight)
        {
            int x = Math.Max(0, Math.Min(rect.X, screenWidth - rect.Width));
            int y = Math.Max(0, Math.Min(rect.Y, screenHeight - rect.Height));
            int width = Math.Min(rect.Width, screenWidth - x);
            int height = Math.Min(rect.Height, screenHeight - y);
            return new Rectangle(x, y, width, height);
        }

        private async Task<Prediction?> GetClosestPrediction(bool useMousePosition = true)
        {
            var cursorPosition = WinAPICaller.GetCursorPosition();
            targetX = Dictionary.dropdownState["Detection Area Type"] == "Closest to Mouse" ? cursorPosition.X : ScreenWidth >> 1;
            targetY = Dictionary.dropdownState["Detection Area Type"] == "Closest to Mouse" ? cursorPosition.Y : ScreenHeight >> 1;
            Rectangle detectionBox = ClampRectangle(new(targetX - (IMAGE_SIZE >> 1), targetY - (IMAGE_SIZE >> 1), IMAGE_SIZE, IMAGE_SIZE), ScreenWidth, ScreenHeight);
            var frame = ScreenGrab(detectionBox);
            if (frame == null) return null;
            var inputArray = BitmapToFloatArray(frame);
            if (_onnxModel == null || inputArray == null) return null;
            var inputTensor = new DenseTensor<float>(inputArray, new int[] { 1, 3, frame.Height, frame.Width });
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("images", inputTensor) };
            using var results = _onnxModel.Run(inputs, _outputNames, _modeloptions);
            var outputTensor = results[0].AsTensor<float>();
            float FovSize = (float)Dictionary.sliderSettings["FOV Size"];
            float fovHalfSize = FovSize * 0.5f;
            float fovMinX = (IMAGE_SIZE - FovSize) * 0.5f;
            float fovMaxX = fovMinX + FovSize;
            float fovMinY = (IMAGE_SIZE - FovSize) * 0.5f;
            float fovMaxY = fovMinY + FovSize;
            var (KDpoints, KDPredictions) = PrepareKDTreeData(outputTensor, detectionBox, fovMinX, fovMaxX, fovMinY, fovMaxY);
            if (KDpoints.Count == 0) return null;
            var tree = new KDTree<double, Prediction>(2, KDpoints.ToArray(), KDPredictions.ToArray(), L2Norm_Squared_Double);
            var nearest = tree.NearestNeighbors(new double[] { IMAGE_SIZE * 0.5, IMAGE_SIZE * 0.5 }, 1);
            if (nearest.Length > 0)
            {
                var nearestPrediction = nearest[0].Item2;
                float translatedXMin = nearestPrediction.Rectangle.X + detectionBox.Left;
                float translatedYMin = nearestPrediction.Rectangle.Y + detectionBox.Top;
                LastDetectionBox = new RectangleF(translatedXMin, translatedYMin, nearestPrediction.Rectangle.Width, nearestPrediction.Rectangle.Height);
                CenterXTranslated = nearestPrediction.CenterXTranslated;
                CenterYTranslated = nearestPrediction.CenterYTranslated;
                return nearestPrediction;
            }
            return null;
        }

        private static (List<double[]>, List<Prediction>) PrepareKDTreeData(Tensor<float> outputTensor, Rectangle detectionBox, float fovMinX, float fovMaxX, float fovMinY, float fovMaxY)
        {
            float minConfidence = (float)Dictionary.sliderSettings["AI Minimum Confidence"] / 100.0f;
            var KDpoints = new List<double[]>(NUM_DETECTIONS);
            var KDpredictions = new List<Prediction>(NUM_DETECTIONS);
            float imgSizeInv = 1.0f / IMAGE_SIZE;
            float boxLeft = detectionBox.Left;
            float boxTop = detectionBox.Top;
            Span<float> tensorSpan = outputTensor.ToArray();

            for (int i = 0; i < NUM_DETECTIONS; i += 4)
            {
                var detections = new ValueTuple<float, float, float, float, float>[4];
                int count = Math.Min(4, NUM_DETECTIONS - i);
                int detCount = 0;

                for (int j = 0; j < count; j++)
                {
                    float objectness = tensorSpan[4 * NUM_DETECTIONS + i + j];
                    if (objectness < minConfidence) continue;
                    detections[detCount++] = (
                        tensorSpan[i + j],
                        tensorSpan[NUM_DETECTIONS + i + j],
                        tensorSpan[2 * NUM_DETECTIONS + i + j],
                        tensorSpan[3 * NUM_DETECTIONS + i + j],
                        objectness
                    );
                }

                for (int k = 0; k < detCount; k++)
                {
                    var (x_center, y_center, width, height, objectness) = detections[k];
                    float x_min = x_center - width * 0.5f;
                    float y_min = y_center - height * 0.5f;
                    float x_max = x_center + width * 0.5f;
                    float y_max = y_center + height * 0.5f;
                    if (x_min < fovMinX || x_max > fovMaxX || y_min < fovMinY || y_max > fovMaxY) continue;

                    KDpoints.Add(new double[] { x_center, y_center });
                    KDpredictions.Add(new Prediction
                    {
                        Rectangle = new RectangleF(x_min, y_min, width, height),
                        Confidence = objectness,
                        CenterXTranslated = (x_center - boxLeft) * imgSizeInv,
                        CenterYTranslated = (y_center - boxTop) * imgSizeInv
                    });
                }
            }

            return (KDpoints, KDpredictions);
        }

        #endregion
        #endregion AI Loop Functions

        #region Screen Capture
        public Bitmap? ScreenGrab(Rectangle detectionBox)
        {
            try
            {
                Bitmap? frame = D3D11Screen(detectionBox);
                if (TPS)
                {
                    Mat _frame = BitmapConverter.ToMat(frame);
                    int half = detectionBox.Height / 2;
                    OpenCvSharp.Rect roi = new OpenCvSharp.Rect(0, detectionBox.Height - half, half, half);
                    _frame[roi].SetTo(new Scalar(0, 0, 0));
                    frame = BitmapConverter.ToBitmap(_frame);
                }
                return frame;
            }
            catch (Exception e)
            {
                //FileManager.LogError("Error capturing screen:" + e);
                return null;
            }
        }
        private Bitmap? D3D11Screen(Rectangle detectionBox)
        {
            try
            {
                if (_device == null || _context == null | _outputDuplication == null)
                {
                    //FileManager.LogError("Device, context, or textures are null, attempting to reinitialize");
                    ReinitializeD3D11();

                    if (_device == null || _context == null || _outputDuplication == null)
                    {
                        throw new InvalidOperationException("Device, context, or textures are still null after reinitialization.");
                    }
                }

                if (_captureBitmap != null)
                {
                    //FileManager.LogInfo("Bitmap was not null, disposing.", true, 1500);
                    _captureBitmap?.Dispose();
                    _captureBitmap = null;
                }

                var result = _outputDuplication!.AcquireNextFrame(500, out var frameInfo, out var desktopResource);

                if (result != Result.Ok)
                {
                    if (result == Vortice.DXGI.ResultCode.DeviceRemoved)
                    {
                        //FileManager.LogError("Device removed, reinitializing D3D11.", true, 1000);
                        ReinitializeD3D11();
                        return null;
                    }

                    //FileManager.LogError("Failed to acquire next frame: " + result + ". Reinitializing...");
                    ReinitializeD3D11();
                    return null;
                }

                using var screenTexture = desktopResource.QueryInterface<ID3D11Texture2D>();

                bool requiresNewResources = _desktopImage == null || _desktopImage.Description.Width != detectionBox.Width || _desktopImage.Description.Height != detectionBox.Height;

                if (requiresNewResources)
                {
                    _desktopImage?.Dispose();

                    var desc = new Texture2DDescription
                    {
                        Width = (uint)detectionBox.Width,
                        Height = (uint)detectionBox.Height,
                        MipLevels = 1,
                        ArraySize = 1,
                        Format = screenTexture.Description.Format,
                        SampleDescription = new SampleDescription(1, 0),
                        Usage = ResourceUsage.Staging,
                        CPUAccessFlags = CpuAccessFlags.Read,
                        BindFlags = BindFlags.None
                    };

                    _desktopImage = _device.CreateTexture2D(desc);
                }
                var box = new Box
                {
                    Left = detectionBox.Left,
                    Top = detectionBox.Top,
                    Front = 0,
                    Right = detectionBox.Right,
                    Bottom = detectionBox.Bottom,
                    Back = 1
                };

                _context!.CopySubresourceRegion(_desktopImage, 0, 0, 0, 0, screenTexture, 0, box);

                if (_desktopImage == null) return null;
                var map = _context.Map(_desktopImage, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);

                var bitmap = new Bitmap(detectionBox.Width, detectionBox.Height, PixelFormat.Format32bppArgb);
                var boundsRect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                var mapDest = bitmap.LockBits(boundsRect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

                unsafe
                {
                    Buffer.MemoryCopy((void*)map.DataPointer, (void*)mapDest.Scan0, mapDest.Stride * mapDest.Height, map.RowPitch * detectionBox.Height);
                    //    var sourcePtr = (byte*)map.DataPointer;
                    //    var destPtr = (byte*)mapDest.Scan0;
                    //    int rowPitch = map.RowPitch;
                    //    int destStride = mapDest.Stride;
                    //    int widthInBytes = detectionBox.Width * 4;

                    //    Buffer.MemoryCopy(sourcePtr, destPtr, widthInBytes * detectionBox.Height, widthInBytes * detectionBox.Height);
                }
                bitmap.UnlockBits(mapDest);
                _context.Unmap(_desktopImage, 0);
                _outputDuplication.ReleaseFrame();

                //FileManager.LogError($"Successfully captured screen with D3D11, width: {detectionBox.Width}, height: {detectionBox.Height}.");
                return bitmap;
            }

            catch (SharpGenException ex)
            {
                //FileManager.LogError("SharpGenException: " + ex);
                ReinitializeD3D11();
                return null;
            }
            catch (Exception e)
            {
                //FileManager.LogError("Error capturing screen:" + e);
                return null;
            }
        }

        #region Reinitialization, Clamping, Misc
        public void ReinitializeD3D11()
        {
            try
            {
                DisposeD311();
                InitializeDirectX();
                //FileManager.LogError("Reinitializing D3D11, timing out for 1000ms");
                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                //FileManager.LogError("Error during D3D11 reinitialization: " + ex);
            }
        }
        #endregion
        #endregion Screen Capture

        #region complicated math

        public static Func<double[], double[], double> L2Norm_Squared_Double = (x, y) =>
        {
            double dist = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                dist += (x[i] - y[i]) * (x[i] - y[i]);
            }

            return dist;
        };

        public static unsafe float[] BitmapToFloatArray(Bitmap image)
        {
            int height = image.Height;
            int width = image.Width;
            int totalPixels = height * width;
            float[] result = new float[3 * totalPixels];
            float multiplier = 1.0f / 255.0f;

            Rectangle rect = new(0, 0, width, height);
            BitmapData bmpData = image.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);

            int stride = bmpData.Stride;
            int offset = stride - (width * 3);

            byte* ptr = (byte*)bmpData.Scan0.ToPointer();
            float* resultPtr = (float*)Marshal.UnsafeAddrOfPinnedArrayElement(result, 0);

            try
            {
                for (int i = 0; i < height; i++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        resultPtr[0] = ptr[2] * multiplier;                // R
                        resultPtr[totalPixels] = ptr[1] * multiplier;      // G
                        resultPtr[2 * totalPixels] = ptr[0] * multiplier;  // B
                        resultPtr++;
                        ptr += 3;
                    }
                    ptr += offset;
                }
            }
            finally
            {
                image.UnlockBits(bmpData);
            }

            return result;
        }


        #endregion complicated math

        public void Dispose()
        {
            // Stop the loop
            _isAiLoopRunning = false;
            if (_aiLoopThread != null && _aiLoopThread.IsAlive)
            {
                if (!_aiLoopThread.Join(TimeSpan.FromSeconds(1)))
                {
                    _aiLoopThread.Interrupt(); // Force join the thread (may error..)
                }
            }

            DisposeResources();
        }
        private void DisposeD311()
        {
            if (_desktopImage != null)
            {
                _desktopImage?.Dispose();
                _desktopImage = null;
            }

            if (_outputDuplication != null)
            {
                _outputDuplication?.Dispose();
                _outputDuplication = null;
            }

            if (_context != null)
            {
                _context?.Dispose();
                _context = null;
            }

            if (_device != null)
            {
                _device?.Dispose();
                _device = null;
            }

        }
        private void DisposeResources()
        {
            DisposeD311();

            _onnxModel?.Dispose();
            _modeloptions?.Dispose();
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