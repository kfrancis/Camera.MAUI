﻿using Camera.MAUI.ZXingHelper;
using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;
using ZXing;
#if IOS || MACCATALYST
using DecodeDataType = UIKit.UIImage;
#elif ANDROID
using DecodeDataType = Android.Graphics.Bitmap;
#elif WINDOWS
using DecodeDataType = Windows.Graphics.Imaging.SoftwareBitmap;
#else
using DecodeDataType = System.Object;
#endif

namespace Camera.MAUI;

public class CameraView : View, ICameraView
{
    public static readonly BindableProperty FlashModeProperty = BindableProperty.Create(nameof(FlashMode), typeof(FlashMode), typeof(CameraView), FlashMode.Disabled);
    public static readonly BindableProperty TorchEnabledProperty = BindableProperty.Create(nameof(TorchEnabled), typeof(bool), typeof(CameraView), false);
    public static readonly BindableProperty CamerasProperty = BindableProperty.Create(nameof(Cameras), typeof(ObservableCollection<CameraInfo>), typeof(CameraView), new ObservableCollection<CameraInfo>());
    public static readonly BindableProperty CameraProperty = BindableProperty.Create(nameof(Camera), typeof(CameraInfo), typeof(CameraView), null, propertyChanged:CameraChanged);
    public static readonly BindableProperty MirroredImageProperty = BindableProperty.Create(nameof(MirroredImage), typeof(bool), typeof(CameraView), false);
    public static readonly BindableProperty BarCodeDetectionEnabledProperty = BindableProperty.Create(nameof(BarCodeDetectionEnabled), typeof(bool), typeof(CameraView), false);
    public static readonly BindableProperty BarCodeDetectionFrameRateProperty = BindableProperty.Create(nameof(BarCodeDetectionFrameRate), typeof(int), typeof(CameraView), 10);
    public static readonly BindableProperty BarCodeOptionsProperty = BindableProperty.Create(nameof(BarCodeOptions), typeof(BarcodeDecodeOptions), typeof(CameraView), new BarcodeDecodeOptions(), propertyChanged:BarCodeOptionsChanged);
    public static readonly BindableProperty ZoomFactorProperty = BindableProperty.Create(nameof(ZoomFactor), typeof(float), typeof(CameraView), 1f);
    public static readonly BindableProperty AutoSnapShotSecondsProperty = BindableProperty.Create(nameof(AutoSnapShotSeconds), typeof(float), typeof(CameraView), 0f);
    public static readonly BindableProperty AutoSnapShotFormatProperty = BindableProperty.Create(nameof(AutoSnapShotFormat), typeof(ImageFormat), typeof(CameraView), ImageFormat.PNG);
    public static readonly BindableProperty SnapShotProperty = BindableProperty.Create(nameof(SnapShot), typeof(ImageSource), typeof(CameraView), null, BindingMode.OneWayToSource);
    public static readonly BindableProperty SnapShotStreamProperty = BindableProperty.Create(nameof(SnapShotStream), typeof(Stream), typeof(CameraView), null, BindingMode.OneWayToSource);
    public static readonly BindableProperty TakeAutoSnapShotProperty = BindableProperty.Create(nameof(TakeAutoSnapShot), typeof(bool), typeof(CameraView), false, propertyChanged:TakeAutoSnapShotChanged);
    public static readonly BindableProperty AutoSnapShotAsImageSourceProperty = BindableProperty.Create(nameof(AutoSnapShotAsImageSource), typeof(bool), typeof(CameraView), false);

    /// <summary>
    /// Flash mode for take a photo. This is a bindable property.
    /// </summary>
    public FlashMode FlashMode
    {
        get { return (FlashMode)GetValue(FlashModeProperty); }
        set { SetValue(FlashModeProperty, value); }
    }
    /// <summary>
    /// Turns the camera torch on and off if available. This is a bindable property.
    /// </summary>
    public bool TorchEnabled
    {
        get { return (bool)GetValue(TorchEnabledProperty); }
        set { SetValue(TorchEnabledProperty, value); }
    }
    /// <summary>
    /// List of available cameras in the device. This is a bindable property.
    /// </summary>
    public ObservableCollection<CameraInfo> Cameras
    {
        get { return (ObservableCollection<CameraInfo>)GetValue(CamerasProperty); }
        set { SetValue(CamerasProperty, value); }
    }
    /// <summary>
    /// Set the camera to use by the controler. This is a bindable property.
    /// </summary>
    public CameraInfo Camera
    {
        get { return (CameraInfo)GetValue(CameraProperty); }
        set { SetValue(CameraProperty, value); }
    }
    /// <summary>
    /// Turns a mirror image of the camera on and off. This is a bindable property.
    /// </summary>
    public bool MirroredImage
    {
        get { return (bool)GetValue(MirroredImageProperty); }
        set { SetValue(MirroredImageProperty, value); }
    }
    /// <summary>
    /// Turns on and off the barcode detection. This is a bindable property.
    /// </summary>
    public bool BarCodeDetectionEnabled
    {
        get { return (bool)GetValue(BarCodeDetectionEnabledProperty); }
        set { SetValue(BarCodeDetectionEnabledProperty, value); }
    }
    /// <summary>
    /// Indicates every how many frames the control tries to detect a barcode in the image. This is a bindable property.
    /// </summary>
    public int BarCodeDetectionFrameRate
    {
        get { return (int)GetValue(BarCodeDetectionFrameRateProperty); }
        set { SetValue(BarCodeDetectionFrameRateProperty, value); }
    }
    /// <summary>
    /// Options for the barcode detection. This is a bindable property.
    /// </summary>
    public BarcodeDecodeOptions BarCodeOptions
    {
        get { return (BarcodeDecodeOptions)GetValue(BarCodeOptionsProperty); }
        set { SetValue(BarCodeOptionsProperty, value); }
    }
    /// <summary>
    /// The zoom factor for the current camera in use. This is a bindable property.
    /// </summary>
    public float ZoomFactor
    {
        get { return (float)GetValue(ZoomFactorProperty); }
        set { SetValue(ZoomFactorProperty, value); }
    }
    /// <summary>
    /// Indicates the minimum zoom factor for the camera in use. This property is refreshed when the "Camera" property change.
    /// </summary>
    public float MinZoomFactor
    {
        get
        {
            if (Camera != null)
                return Camera.MinZoomFactor;
            else
                return 1f;
        }
    }
    /// <summary>
    /// Indicates the maximum zoom factor for the camera in use. This property is refreshed when the "Camera" property change.
    /// </summary>
    public float MaxZoomFactor
    {
        get
        {
            if (Camera != null)
                return Camera.MaxZoomFactor;
            else
                return 1f;
        }
    }
    /// <summary>
    /// Sets how often the SnapShot property is updated in seconds.
    /// Default 0: no snapshots are taken
    /// WARNING! A low frequency directly impacts over control performance and memory usage (with AutoSnapShotAsImageSource = true)
    /// </summary>
    public float AutoSnapShotSeconds
    {
        get { return (float)GetValue(AutoSnapShotSecondsProperty); }
        set { SetValue(AutoSnapShotSecondsProperty, value); }
    }
    /// <summary>
    /// Sets the snaphost image format
    /// </summary>
    public ImageFormat AutoSnapShotFormat
    {
        get { return (ImageFormat)GetValue(AutoSnapShotFormatProperty); }
        set { SetValue(AutoSnapShotFormatProperty, value); }
    }
    /// <summary>
    /// Refreshes according to the frequency set in the AutoSnapShotSeconds property (if AutoSnapShotAsImageSource is set to true)
    /// or when GetSnapShot is called or TakeAutoSnapShot is set to true
    /// </summary>
    public ImageSource SnapShot
    {
        get { return (ImageSource)GetValue(SnapShotProperty); }
        private set { SetValue(SnapShotProperty, value); }
    }
    /// <summary>
    /// Refreshes according to the frequency set in the AutoSnapShotSeconds property or when GetSnapShot is called.
    /// WARNING. Each time a snapshot is made, the previous stream is disposed.
    /// </summary>
    public Stream SnapShotStream
    {
        get { return (Stream)GetValue(SnapShotStreamProperty); }
        internal set { SetValue(SnapShotStreamProperty, value); }
    }
    /// <summary>
    /// Change from false to true refresh SnapShot property
    /// </summary>
    public bool TakeAutoSnapShot
    {
        get { return (bool)GetValue(TakeAutoSnapShotProperty); }
        set { SetValue(TakeAutoSnapShotProperty, value); }
    }
    /// <summary>
    /// If true SnapShot property is refreshed according to the frequency set in the AutoSnapShotSeconds property
    /// </summary>
    public bool AutoSnapShotAsImageSource
    {
        get { return (bool)GetValue(AutoSnapShotAsImageSourceProperty); }
        set { SetValue(AutoSnapShotAsImageSourceProperty, value); }
    }
    public delegate void BarcodeResultHandler(object sender, BarcodeEventArgs args);
    /// <summary>
    /// Event launched every time a code is detected in the image if "BarCodeDetectionEnabled" is set to true.
    /// </summary>
    public event BarcodeResultHandler BarcodeDetected;
    /// <summary>
    /// Event launched when "Cameras" property has been loaded.
    /// </summary>
    public event EventHandler CamerasLoaded;

    private readonly BarcodeReaderGeneric BarcodeReader;
    internal DateTime lastSnapshot = DateTime.Now;

    public CameraView()
    {
        BarcodeReader = new BarcodeReaderGeneric();
        HandlerChanged += CameraView_HandlerChanged;
    }
    private void CameraView_HandlerChanged(object sender, EventArgs e)
    {
        if (Handler != null) CamerasLoaded?.Invoke(this, EventArgs.Empty);
    }
    internal void RefreshSnapshot(ImageSource img)
    {
        if (AutoSnapShotAsImageSource)
        {
            SnapShot = img;
            OnPropertyChanged(nameof(SnapShot));
        }
        OnPropertyChanged(nameof(SnapShotStream));
        lastSnapshot = DateTime.Now;
    }

    internal void DecodeBarcode(DecodeDataType data)
    {
        LuminanceSource lumSource = default;
#if ANDROID
        lumSource = new Camera.MAUI.Platforms.Android.BitmapLuminanceSource(data);
#elif IOS || MACCATALYST
        lumSource = new Camera.MAUI.ZXingHelper.RGBLuminanceSource(data);
#elif WINDOWS
        lumSource = new Camera.MAUI.Platforms.Windows.SoftwareBitmapLuminanceSource(data);
#endif
        try
        {
            Result[] results = null;
            if (BarCodeOptions.ReadMultipleCodes)
                results = BarcodeReader.DecodeMultiple(lumSource);
            else
            {
                var result = BarcodeReader.Decode(lumSource);
                if (result != null) results = new Result[] { result };
            }
            if (results?.Length > 0)
            {
                BarcodeDetected?.Invoke(this, new BarcodeEventArgs { Result = results });
            }
        }
        catch {}
    }
    private static void CameraChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (newValue != null && oldValue != newValue && bindable is CameraView cameraView && newValue is CameraInfo)
        {
            cameraView.OnPropertyChanged(nameof(MinZoomFactor));
            cameraView.OnPropertyChanged(nameof(MaxZoomFactor));
        }
    }
    private static void TakeAutoSnapShotChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if ((bool)newValue && !(bool)oldValue && bindable is CameraView cameraView)
        {
            cameraView.RefreshSnapshot(cameraView.GetSnapShot(cameraView.AutoSnapShotFormat));
        }
    }
    private static void BarCodeOptionsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (newValue != null && oldValue != newValue && bindable is CameraView cameraView && newValue is BarcodeDecodeOptions options)
        {
            cameraView.BarcodeReader.AutoRotate = options.AutoRotate;
            if (options.CharacterSet != string.Empty) cameraView.BarcodeReader.Options.CharacterSet = options.CharacterSet;
            cameraView.BarcodeReader.Options.PossibleFormats = options.PossibleFormats;
            cameraView.BarcodeReader.Options.TryHarder = options.TryHarder;
            cameraView.BarcodeReader.Options.TryInverted = options.TryInverted;
            cameraView.BarcodeReader.Options.PureBarcode = options.PureBarcode;
        }
    }
    /// <summary>
    /// Start playback of the selected camera async. "Camera" property must not be null.
    /// </summary>
    public async Task<CameraResult> StartCameraAsync()
    {
        CameraResult result = CameraResult.AccessError;
        if (Camera != null && Handler != null && Handler is CameraViewHandler handler)
        {
            result = await handler.StartCameraAsync();
            if (result == CameraResult.Success)
            {
                OnPropertyChanged(nameof(MinZoomFactor));
                OnPropertyChanged(nameof(MaxZoomFactor));
            }
        }
        return result;
    }
    /// <summary>
    /// Stop playback of the selected camera async.
    /// </summary>
    public async Task<CameraResult> StopCameraAsync()
    {
        CameraResult result = CameraResult.AccessError;
        if (Handler != null && Handler is CameraViewHandler handler)
        {
            result = await handler.StopCameraAsync();
        }
        return result;
    }
    /// <summary>
    /// Takes a capture form the active camera playback.
    /// </summary>
    /// <param name="imageFormat">The capture image format</param>
    public ImageSource GetSnapShot(ImageFormat imageFormat = ImageFormat.PNG)
    {
        if (Handler != null && Handler is CameraViewHandler handler)
        {
            SnapShot = handler.GetSnapShot(imageFormat);
        }
        return SnapShot;
    }
    /// <summary>
    /// Saves a capture form the active camera playback in a file
    /// </summary>
    /// <param name="imageFormat">The capture image format</param>
    /// <param name="SnapFilePath">Full path for the file</param>
    public async Task<bool> SaveSnapShot(ImageFormat imageFormat, string SnapFilePath)
    {
        bool result = false;
        if (Handler != null && Handler is CameraViewHandler handler)
        {
            result = await handler.SaveSnapShot(imageFormat, SnapFilePath);
        }
        return result;
    }
}