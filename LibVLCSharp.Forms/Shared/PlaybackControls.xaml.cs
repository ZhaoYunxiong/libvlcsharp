﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Forms.Shared.Resources;
using LibVLCSharp.Shared;
using LibVLCSharp.Shared.Structures;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace LibVLCSharp.Forms.Shared
{
    /// <summary>
    /// Represents the playback controls for a <see cref="LibVLCSharp.Shared.MediaPlayer"/>.
    /// </summary>
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PlaybackControls : TemplatedView
    {
        private const string AudioSelectionAvailableState = "AudioSelectionAvailable";
        private const string AudioSelectionUnavailableState = "AudioSelectionUnavailable";
        private const string ClosedCaptionsSelectionAvailableState = "ClosedCaptionsSelectionAvailable";
        private const string ClosedCaptionsSelectionUnavailableState = "ClosedCaptionsSelectionUnavailable";
        private const string PlayState = "PlayState";
        private const string PauseState = "PauseState";
        private const string PauseAvailableState = "PauseAvailable";
        private const string PauseUnavailableState = "PauseUnavailable";
        private const string SeekAvailableState = "SeekAvailable";
        private const string SeekUnavailableState = "SeekUnavailable";
        private const string CastAvailableState = "CastAvailable";
        private const string CastUnavailableState = "CastUnavailable";

        /// <summary>
        /// Initializes a new instance of <see cref="PlaybackControls"/> class.
        /// </summary>
        public PlaybackControls()
        {
            InitializeComponent();

            ButtonColor = (Color)(Resources[nameof(ButtonColor)] ?? Color.Transparent);
            Foreground = (Color)(Resources[nameof(Foreground)] ?? Color.White);
            MainColor = (Color)(Resources[nameof(MainColor)] ?? Color.Transparent);
            AudioTracksSelectionButtonStyle = Resources[nameof(AudioTracksSelectionButtonStyle)] as Style;
            BufferingProgressBarStyle = Resources[nameof(BufferingProgressBarStyle)] as Style;
            ButtonBarStyle = Resources[nameof(ButtonBarStyle)] as Style;
            CastButtonStyle = Resources[nameof(CastButtonStyle)] as Style;
            ClosedCaptionsSelectionButtonStyle = Resources[nameof(ClosedCaptionsSelectionButtonStyle)] as Style;
            ControlsPanelStyle = Resources[nameof(ControlsPanelStyle)] as Style;
            MessageStyle = Resources[nameof(MessageStyle)] as Style;
            PlayPauseButtonStyle = Resources[nameof(PlayPauseButtonStyle)] as Style;
            RemainingTimeLabelStyle = Resources[nameof(RemainingTimeLabelStyle)] as Style;
            ElapsedTimeLabelStyle = Resources[nameof(ElapsedTimeLabelStyle)] as Style;
            SeekBarStyle = Resources[nameof(SeekBarStyle)] as Style;
            StopButtonStyle = Resources[nameof(StopButtonStyle)] as Style;
            AspectRatioButtonStyle = Resources[nameof(AspectRatioButtonStyle)] as Style;
            RewindButtonStyle = Resources[nameof(RewindButtonStyle)] as Style;
            SeekButtonStyle = Resources[nameof(SeekButtonStyle)] as Style;

            RendererItems.CollectionChanged += RendererItems_CollectionChanged;
            FadeOutTimer = new Timer(obj => FadeOut());
        }

        private void RendererItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            UpdateCastAvailability();
        }

        private Button AudioTracksSelectionButton { get; set; }
        private Button CastButton { get; set; }
        private Button ClosedCaptionsSelectionButton { get; set; }
        private VisualElement ControlsPanel { get; set; }
        private Button PlayPauseButton { get; set; }
        private Label RemainingTimeLabel { get; set; }
        private Label ElapsedTimeLabel { get; set; }
        private Label AspectRatioLabel { get; set; }

        private Slider SeekBar { get; set; }

        private bool Initialized { get; set; }
        private IPowerManager PowerManager => DependencyService.Get<IPowerManager>();
        private ISystemUI SystemUI => DependencyService.Get<ISystemUI>();
        private AspectRatio CurrentAspectRatio = AspectRatio.Original;

        private Timer FadeOutTimer { get; }
        private bool FadeOutEnabled { get; set; } = true;
        private const int SEEK_OFFSET = 2000;
        private bool RemoteRendering { get; set; } = false;
        private const string Disconnect = "Disconnect";
        private const string Cancel = "Cancel";

        private readonly Dictionary<AspectRatio, string> AspectRatioLabels = new Dictionary<AspectRatio, string>
        {
            { AspectRatio.Original, "Original" },
            { AspectRatio.FitScreen, "Fit Screen" },
            { AspectRatio.BestFit, "Best Fit" },
            { AspectRatio.Fill, "Fill" },
            { AspectRatio._16_9, "16:9" },
            { AspectRatio._4_3, "4:3" }
        };

        /// <summary>
        /// Identifies the <see cref="ButtonColor"/> dependency property.
        /// </summary>
        public static readonly BindableProperty ButtonColorProperty = BindableProperty.Create(nameof(ButtonColor), typeof(Color),
            typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the button color.
        /// </summary>
        public Color ButtonColor
        {
            get => (Color)GetValue(ButtonColorProperty);
            set => SetValue(ButtonColorProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Foreground"/> dependency property.
        /// </summary>
        public static readonly BindableProperty ForegroundProperty = BindableProperty.Create(nameof(Foreground), typeof(Color),
            typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the button color.
        /// </summary>
        public Color Foreground
        {
            get => (Color)GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MainColor"/> dependency property.
        /// </summary>
        public static readonly BindableProperty MainColorProperty = BindableProperty.Create(nameof(MainColor), typeof(Color),
            typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the main color.
        /// </summary>
        public Color MainColor
        {
            get => (Color)GetValue(MainColorProperty);
            set => SetValue(MainColorProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AudioTracksSelectionButtonStyle"/> dependency property.
        /// </summary>
        public static readonly BindableProperty AudioTracksSelectionButtonStyleProperty = BindableProperty.Create(
            nameof(AudioTracksSelectionButtonStyle), typeof(Style), typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the audio tracks selection button style.
        /// </summary>
        public Style AudioTracksSelectionButtonStyle
        {
            get => (Style)GetValue(AudioTracksSelectionButtonStyleProperty);
            set => SetValue(AudioTracksSelectionButtonStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BufferingProgressBarStyle"/> dependency property.
        /// </summary>
        public static readonly BindableProperty BufferingProgressBarStyleProperty = BindableProperty.Create(nameof(BufferingProgressBarStyle),
            typeof(Style), typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the controls panel style.
        /// </summary>
        public Style BufferingProgressBarStyle
        {
            get => (Style)GetValue(BufferingProgressBarStyleProperty);
            set => SetValue(BufferingProgressBarStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ButtonBarStyle"/> dependency property.
        /// </summary>
        public static readonly BindableProperty ButtonBarStyleProperty = BindableProperty.Create(nameof(ButtonBarStyle), typeof(Style),
            typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the button bar style.
        /// </summary>
        public Style ButtonBarStyle
        {
            get => (Style)GetValue(ButtonBarStyleProperty);
            set => SetValue(ButtonBarStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CastButtonStyle"/> dependency property.
        /// </summary>
        public static readonly BindableProperty CastButtonStyleProperty = BindableProperty.Create(nameof(CastButtonStyle), typeof(Style),
            typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the cast button style.
        /// </summary>
        public Style CastButtonStyle
        {
            get => (Style)GetValue(CastButtonStyleProperty);
            set => SetValue(CastButtonStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ClosedCaptionsSelectionButtonStyle"/> dependency property.
        /// </summary>
        public static readonly BindableProperty ClosedCaptionsSelectionButtonStyleProperty = BindableProperty.Create(
            nameof(ClosedCaptionsSelectionButtonStyle), typeof(Style), typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the closed captions selection button style.
        /// </summary>
        public Style ClosedCaptionsSelectionButtonStyle
        {
            get => (Style)GetValue(ClosedCaptionsSelectionButtonStyleProperty);
            set => SetValue(ClosedCaptionsSelectionButtonStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ControlsPanelStyle"/> dependency property.
        /// </summary>
        public static readonly BindableProperty ControlsPanelStyleProperty = BindableProperty.Create(nameof(ControlsPanelStyle), typeof(Style),
            typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the controls panel style.
        /// </summary>
        public Style ControlsPanelStyle
        {
            get => (Style)GetValue(ControlsPanelStyleProperty);
            set => SetValue(ControlsPanelStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MessageStyle"/> dependency property.
        /// </summary>
        public static readonly BindableProperty MessageStyleProperty = BindableProperty.Create(nameof(MessageStyle), typeof(Style),
            typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the message style.
        /// </summary>
        public Style MessageStyle
        {
            get => (Style)GetValue(MessageStyleProperty);
            set => SetValue(MessageStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PlayPauseButtonStyle"/> dependency property.
        /// </summary>
        public static readonly BindableProperty PlayPauseButtonStyleProperty = BindableProperty.Create(nameof(PlayPauseButtonStyle), typeof(Style),
            typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the play/pause button style.
        /// </summary>
        public Style PlayPauseButtonStyle
        {
            get => (Style)GetValue(PlayPauseButtonStyleProperty);
            set => SetValue(PlayPauseButtonStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="RemainingTimeLabelStyle"/> dependency property.
        /// </summary>
        public static readonly BindableProperty RemainingTimeLabelStyleProperty = BindableProperty.Create(nameof(RemainingTimeLabelStyle),
            typeof(Style), typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the remaining time label style.
        /// </summary>
        public Style RemainingTimeLabelStyle
        {
            get => (Style)GetValue(RemainingTimeLabelStyleProperty);
            set => SetValue(RemainingTimeLabelStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ElapsedTimeLabelStyle"/> dependency property.
        /// </summary>
        public static readonly BindableProperty ElapsedTimeLabelStyleProperty = BindableProperty.Create(nameof(ElapsedTimeLabelStyle),
            typeof(Style), typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the elapsed time label style.
        /// </summary>
        public Style ElapsedTimeLabelStyle
        {
            get => (Style)GetValue(ElapsedTimeLabelStyleProperty);
            set => SetValue(ElapsedTimeLabelStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SeekBarStyle"/> dependency property.
        /// </summary>
        public static readonly BindableProperty SeekBarStyleProperty = BindableProperty.Create(nameof(SeekBarStyle), typeof(Style),
            typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the seek bar style.
        /// </summary>
        public Style SeekBarStyle
        {
            get => (Style)GetValue(SeekBarStyleProperty);
            set => SetValue(SeekBarStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="StopButtonStyle"/> dependency property.
        /// </summary>
        public static readonly BindableProperty StopButtonStyleProperty = BindableProperty.Create(nameof(StopButtonStyle), typeof(Style),
            typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the stop button style.
        /// </summary>
        public Style StopButtonStyle
        {
            get => (Style)GetValue(StopButtonStyleProperty);
            set => SetValue(StopButtonStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="VideoView"/> dependency property.
        /// </summary>
        public static readonly BindableProperty VideoViewProperty = BindableProperty.Create(nameof(VideoView), typeof(VideoView),
            typeof(PlaybackControls), propertyChanged: null);

        /// <summary>
        /// Gets or sets the associated <see cref="VideoView"/>.
        /// </summary>
        /// <remarks>It is only useful to set this property for the aspect ratio feature.</remarks>
        public VideoView VideoView
        {
            get => (VideoView)GetValue(VideoViewProperty);
            set => SetValue(VideoViewProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AspectRatioButtonStyle"/> dependency property.
        /// </summary>
        public static readonly BindableProperty AspectRatioButtonStyleProperty = BindableProperty.Create(nameof(AspectRatioButtonStyle), typeof(Style),
            typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the aspect ratio button style.
        /// </summary>
        public Style AspectRatioButtonStyle
        {
            get => (Style)GetValue(AspectRatioButtonStyleProperty);
            set => SetValue(AspectRatioButtonStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="RewindButtonStyle"/> dependency property.
        /// </summary>
        public static readonly BindableProperty RewindButtonStyleProperty = BindableProperty.Create(nameof(RewindButtonStyle), typeof(Style),
            typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the rewind button style.
        /// </summary>
        public Style RewindButtonStyle
        {
            get => (Style)GetValue(RewindButtonStyleProperty);
            set => SetValue(RewindButtonStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SeekButtonStyle"/> dependency property.
        /// </summary>
        public static readonly BindableProperty SeekButtonStyleProperty = BindableProperty.Create(nameof(SeekButtonStyle), typeof(Style),
            typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the rewind button style.
        /// </summary>
        public Style SeekButtonStyle
        {
            get => (Style)GetValue(SeekButtonStyleProperty);
            set => SetValue(SeekButtonStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ButtonBarStartArea"/> dependency property.
        /// </summary>
        public static readonly BindableProperty ButtonBarStartAreaProperty = BindableProperty.Create(nameof(ButtonBarStartArea), typeof(View),
            typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the view in the button bar start area.
        /// </summary>
        public View ButtonBarStartArea
        {
            get => (View)GetValue(ButtonBarStartAreaProperty);
            set => SetValue(ButtonBarStartAreaProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ButtonBarEndArea"/> dependency property.
        /// </summary>
        public static readonly BindableProperty ButtonBarEndAreaProperty = BindableProperty.Create(nameof(ButtonBarEndArea), typeof(View),
            typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the view in the button bar end area.
        /// </summary>
        public View ButtonBarEndArea
        {
            get => (View)GetValue(ButtonBarEndAreaProperty);
            set => SetValue(ButtonBarEndAreaProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="LibVLC"/> dependency property.
        /// </summary>
        public static readonly BindableProperty LibVLCProperty = BindableProperty.Create(nameof(LibVLC), typeof(LibVLC), typeof(PlaybackControls),
            propertyChanged: LibVLCPropertyChanged);
        /// <summary>
        /// Gets or sets the <see cref="LibVLCSharp.Shared.LibVLC"/> instance.
        /// </summary>
        public LibVLC LibVLC
        {
            get => (LibVLC)GetValue(LibVLCProperty);
            set => SetValue(LibVLCProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MediaPlayer"/> dependency property.
        /// </summary>
        public static readonly BindableProperty MediaPlayerProperty = BindableProperty.Create(nameof(MediaPlayer),
            typeof(LibVLCSharp.Shared.MediaPlayer), typeof(PlaybackControls), propertyChanged: MediaPlayerPropertyChanged);
        /// <summary>
        /// Gets or sets the <see cref="LibVLCSharp.Shared.MediaPlayer"/> instance.
        /// </summary>
        public LibVLCSharp.Shared.MediaPlayer MediaPlayer
        {
            get => (LibVLCSharp.Shared.MediaPlayer)GetValue(MediaPlayerProperty);
            set => SetValue(MediaPlayerProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="BufferingProgress"/> dependency property.
        /// </summary>
        public static readonly BindableProperty BufferingProgressProperty = BindableProperty.Create(nameof(BufferingProgress), typeof(double),
            typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets a value corresponding to the buffering progress.
        /// </summary>
        public double BufferingProgress
        {
            get => (double)GetValue(BufferingProgressProperty);
            set => SetValue(BufferingProgressProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ErrorMessage"/> dependency property.
        /// </summary>
        public static readonly BindableProperty ErrorMessageProperty = BindableProperty.Create(nameof(ErrorMessage), typeof(string),
            typeof(PlaybackControls));
        /// <summary>
        /// Gets the last error message.
        /// </summary>
        public string ErrorMessage
        {
            get => (string)GetValue(ErrorMessageProperty);
            private set => SetValue(ErrorMessageProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="KeepScreenOn"/> dependency property.
        /// </summary>
        public static readonly BindableProperty KeepScreenOnProperty = BindableProperty.Create(nameof(KeepScreenOn), typeof(bool),
            typeof(PlaybackControls), true, propertyChanged: KeepScreenOnPropertyChanged);
        /// <summary>
        /// Gets or sets a value indicating whether the screen must be kept on when playing.
        /// </summary>
        public bool KeepScreenOn
        {
            get => (bool)GetValue(KeepScreenOnProperty);
            set => SetValue(KeepScreenOnProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Position"/> dependency property.
        /// </summary>
        public static readonly BindableProperty PositionProperty = BindableProperty.Create(nameof(Position), typeof(TimeSpan),
            typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the playback position within the media.
        /// </summary>
        public TimeSpan Position
        {
            get => (TimeSpan)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ResourceManager"/> dependency property.
        /// </summary>
        public static readonly BindableProperty ResourceManagerProperty = BindableProperty.Create(nameof(ResourceManager), typeof(ResourceManager),
            typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets the resource manager to localize strings.
        /// </summary>
        public ResourceManager ResourceManager
        {
            get => (ResourceManager)GetValue(ResourceManagerProperty) ?? Strings.ResourceManager;
            set => SetValue(ResourceManagerProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowAndHideAutomatically"/> dependency property.
        /// </summary>
        public static readonly BindableProperty ShowAndHideAutomaticallyProperty = BindableProperty.Create(nameof(ShowAndHideAutomatically),
            typeof(bool), typeof(PlaybackControls), true);
        /// <summary>
        /// Gets or sets a value that indicates whether the controls are shown and hidden automatically.
        /// </summary>
        public bool ShowAndHideAutomatically
        {
            get => (bool)GetValue(ShowAndHideAutomaticallyProperty);
            set => SetValue(ShowAndHideAutomaticallyProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsAudioTracksSelectionButtonVisible"/> dependency property.
        /// </summary>
        public static readonly BindableProperty IsAudioTracksSelectionButtonVisibleProperty = BindableProperty.Create(
            nameof(IsAudioTracksSelectionButtonVisible), typeof(bool), typeof(PlaybackControls), true,
            propertyChanged: IsAudioTracksSelectionButtonVisiblePropertyChanged);
        /// <summary>
        /// Gets or sets a value indicating whether the audio tracks selection button is shown.
        /// </summary>
        public bool IsAudioTracksSelectionButtonVisible
        {
            get => (bool)GetValue(IsAudioTracksSelectionButtonVisibleProperty);
            set => SetValue(IsAudioTracksSelectionButtonVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsCastButtonVisible"/> dependency property.
        /// </summary>
        public static readonly BindableProperty IsCastButtonVisibleProperty = BindableProperty.Create(nameof(IsCastButtonVisible), typeof(bool),
            typeof(PlaybackControls), true, propertyChanged: IsCastButtonVisiblePropertyChanged);
        /// <summary>
        /// Gets or sets a value indicating whether the cast button is shown.
        /// </summary>
        public bool IsCastButtonVisible
        {
            get => (bool)GetValue(IsCastButtonVisibleProperty);
            set => SetValue(IsCastButtonVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsClosedCaptionsSelectionButtonVisible"/> dependency property.
        /// </summary>
        public static readonly BindableProperty IsClosedCaptionsSelectionButtonVisibleProperty = BindableProperty.Create(
            nameof(IsClosedCaptionsSelectionButtonVisible), typeof(bool), typeof(PlaybackControls), true,
            propertyChanged: IsClosedCaptionsSelectionButtonVisiblePropertyChanged);
        /// <summary>
        /// Gets or sets a value indicating whether the closed captions selection button is shown.
        /// </summary>
        public bool IsClosedCaptionsSelectionButtonVisible
        {
            get => (bool)GetValue(IsClosedCaptionsSelectionButtonVisibleProperty);
            set => SetValue(IsClosedCaptionsSelectionButtonVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsPlayPauseButtonVisible"/> dependency property.
        /// </summary>
        public static readonly BindableProperty IsPlayPauseButtonVisibleProperty = BindableProperty.Create(nameof(IsPlayPauseButtonVisible),
            typeof(bool), typeof(PlaybackControls), true, propertyChanged: IsPlayPauseButtonVisiblePropertyChanged);
        /// <summary>
        /// Gets or sets a value indicating whether the play/pause button is shown.
        /// </summary>
        public bool IsPlayPauseButtonVisible
        {
            get => (bool)GetValue(IsPlayPauseButtonVisibleProperty);
            set => SetValue(IsPlayPauseButtonVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsSeekEnabled"/> dependency property.
        /// </summary>
        public static readonly BindableProperty IsSeekEnabledProperty = BindableProperty.Create(nameof(IsSeekEnabled), typeof(bool),
            typeof(PlaybackControls), true);
        /// <summary>
        /// Gets or sets a value that indicates whether a user can use the seek bar to find a location in the media.
        /// </summary>
        public bool IsSeekEnabled
        {
            get => (bool)GetValue(IsSeekEnabledProperty);
            set => SetValue(IsSeekEnabledProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsSeekBarVisible"/> dependency property.
        /// </summary>
        public static readonly BindableProperty IsSeekBarVisibleProperty = BindableProperty.Create(nameof(IsSeekBarVisible), typeof(bool),
            typeof(PlaybackControls), true);
        /// <summary>
        /// Gets or sets a value that indicates whether the seek bar is shown.
        /// </summary>
        public bool IsSeekBarVisible
        {
            get => (bool)GetValue(IsSeekBarVisibleProperty);
            set => SetValue(IsSeekBarVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsStopButtonVisible"/> dependency property.
        /// </summary>
        public static readonly BindableProperty IsStopButtonVisibleProperty = BindableProperty.Create(nameof(IsStopButtonVisible), typeof(bool),
            typeof(PlaybackControls));
        /// <summary>
        /// Gets or sets a value that indicates whether the stop button is shown.
        /// </summary>
        public bool IsStopButtonVisible
        {
            get => (bool)GetValue(IsStopButtonVisibleProperty);
            set => SetValue(IsStopButtonVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsAspectRatioButtonVisible"/> dependency property.
        /// </summary>
        public static readonly BindableProperty IsAspectRatioButtonVisibleProperty = BindableProperty.Create(nameof(IsAspectRatioButtonVisible),
            typeof(bool), typeof(PlaybackControls), true);
        /// <summary>
        /// Gets or sets a value indicating whether the aspect ratio button is shown.
        /// </summary>
        public bool IsAspectRatioButtonVisible
        {
            get => (bool)GetValue(IsAspectRatioButtonVisibleProperty);
            set => SetValue(IsAspectRatioButtonVisibleProperty, value);
        }

        bool _enableRendererDiscovery = true;
        /// <summary>
        /// Enable or disable renderer discovery
        /// </summary>
        internal bool EnableRendererDiscovery
        {
            get => _enableRendererDiscovery;
            set
            {
                _enableRendererDiscovery = value;
                IsCastButtonVisible = _enableRendererDiscovery;
                UpdateCastAvailability();
                ResetRendererDiscovery();
            }
        }
        
        /// <summary>
        /// Identifies the <see cref="IsRewindButtonVisible"/> dependency property.
        /// </summary>s
        public static readonly BindableProperty IsRewindButtonVisibleProperty = BindableProperty.Create(nameof(IsRewindButtonVisible),
            typeof(bool), typeof(PlaybackControls), true);
        /// <summary>
        /// Gets or sets a value indicating whether the rewind button is shown.
        /// </summary>
        public bool IsRewindButtonVisible
        {
            get => (bool)GetValue(IsRewindButtonVisibleProperty);
            set => SetValue(IsRewindButtonVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsSeekButtonVisible"/> dependency property.
        /// </summary>
        public static readonly BindableProperty IsSeekButtonVisibleProperty = BindableProperty.Create(nameof(IsSeekButtonVisible),
            typeof(bool), typeof(PlaybackControls), true);
        /// <summary>
        /// Gets or sets a value indicating whether the seek button is shown.
        /// </summary>
        public bool IsSeekButtonVisible
        {
            get => (bool)GetValue(IsSeekButtonVisibleProperty);
            set => SetValue(IsSeekButtonVisibleProperty, value);
        }

        /// <summary>
        /// Called when the <see cref="Element.Parent"/> property has changed.
        /// </summary>
        protected override void OnParentSet()
        {
            base.OnParentSet();
            if (Parent != null && !Initialized)
            {
                Initialized = true;
                OnApplyTemplate();
                Reset();
            }
        }

        private void OnApplyTemplate()
        {
            AudioTracksSelectionButton = SetClickEventHandler(nameof(AudioTracksSelectionButton), AudioTracksSelectionButton_Clicked, true);
            CastButton = SetClickEventHandler(nameof(CastButton), CastButton_Clicked);
            ClosedCaptionsSelectionButton = SetClickEventHandler(nameof(ClosedCaptionsSelectionButton), ClosedCaptionsSelectionButton_Clicked,
                true);
            PlayPauseButton = SetClickEventHandler(nameof(PlayPauseButton), PlayPauseButton_Clicked);
            SetClickEventHandler("StopButton", StopButton_Clicked, true);
            SetClickEventHandler("AspectRatioButton", AspectRatioButton_Clicked, true);
            ControlsPanel = this.FindChild<VisualElement>(nameof(ControlsPanel));
            SeekBar = this.FindChild<Slider>(nameof(SeekBar));
            RemainingTimeLabel = this.FindChild<Label>(nameof(RemainingTimeLabel));
            ElapsedTimeLabel = this.FindChild<Label>(nameof(ElapsedTimeLabel));
            AspectRatioLabel = this.FindChild<Label>(nameof(AspectRatioLabel));
            SetClickEventHandler("RewindButton", RewindButton_Clicked);
            SetClickEventHandler("SeekButton", SeekButton_Clicked);

            if (SeekBar != null)
            {
                SeekBar.ValueChanged += SeekBar_ValueChanged;
            }
        }

        private static void IsAudioTracksSelectionButtonVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((PlaybackControls)bindable).UpdateAudioTracksSelectionAvailability();
        }

        private static void IsClosedCaptionsSelectionButtonVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((PlaybackControls)bindable).UpdateClosedCaptionsTracksSelectionAvailability();
        }

        private static void IsPlayPauseButtonVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((PlaybackControls)bindable).UpdatePauseAvailability();
        }

        private static void IsCastButtonVisiblePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((PlaybackControls)bindable).UpdateCastAvailability();
        }

        private static void KeepScreenOnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((PlaybackControls)bindable).UpdateKeepScreenOn((bool)newValue);
        }

        private static void LibVLCPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var playbackControls = (PlaybackControls)bindable;
            playbackControls.UpdateCastAvailability();
            playbackControls.UpdateErrorMessage();
            playbackControls.ResetRendererDiscovery();
        }

        private void ResetRendererDiscovery()
        {
            ClearRenderer();

            if(EnableRendererDiscovery)
                FindRenderers();
        }

        private static void MediaPlayerPropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            ((PlaybackControls)bindable).OnMediaPlayerChanged((LibVLCSharp.Shared.MediaPlayer)oldValue, (LibVLCSharp.Shared.MediaPlayer)newValue);
        }

        private void MediaPlayer_Buffering(object sender, MediaPlayerBufferingEventArgs e)
        {
            var value = (int)e.Cache / 100.0d;
            if (BufferingProgress != value)
            {
                Device.BeginInvokeOnMainThread(() => BufferingProgress = value);
            }
        }

        private void MediaPlayer_EncounteredError(object sender, EventArgs e)
        {
            UpdateState(VLCState.Error);
        }

        private void MediaPlayer_EndReached(object sender, EventArgs e)
        {
            UpdateState(VLCState.Ended);
        }

        private void MediaPlayer_LengthChanged(object sender, MediaPlayerLengthChangedEventArgs e)
        {
            UpdateTime(MediaPlayer?.Position);
        }

        private void MediaPlayer_MediaChanged(object sender, MediaPlayerMediaChangedEventArgs e)
        {
            Reset();
        }

        private void MediaPlayer_NothingSpecial(object sender, EventArgs e)
        {
            UpdateState(VLCState.NothingSpecial);
        }

        private void MediaPlayer_PausableChanged(object sender, MediaPlayerPausableChangedEventArgs e)
        {
            UpdatePauseAvailability(e.Pausable == 1);
        }

        private void MediaPlayer_Paused(object sender, EventArgs e)
        {
            UpdateState(VLCState.Paused);
        }

        private void MediaPlayer_Playing(object sender, EventArgs e)
        {
            UpdateState(VLCState.Playing);
        }

        private void MediaPlayer_PositionChanged(object sender, MediaPlayerPositionChangedEventArgs e)
        {
            UpdatePosition(e.Position);
        }

        private void MediaPlayer_SeekableChanged(object sender, MediaPlayerSeekableChangedEventArgs e)
        {
            UpdateSeekAvailability(e.Seekable == 1);
        }

        private void MediaPlayer_Stopped(object sender, EventArgs e)
        {
            UpdateState(VLCState.Stopped);
        }

        private void MediaPlayer_TracksChanged(object sender, EventArgs e)
        {
            UpdateAudioTracksSelectionAvailability();
            UpdateClosedCaptionsTracksSelectionAvailability();
        }

        private async void AudioTracksSelectionButton_Clicked(object sender, EventArgs e)
        {
            await SelectTrackAsync(TrackType.Audio, ResourceManager.GetString(nameof(Strings.AudioTracks)), m => m.AudioTrack,
                (m, id) => m.SetAudioTrack(id));
        }

        private async void CastButton_Clicked(object sender, EventArgs e)
        {
            var libVLC = LibVLC;
            if (libVLC != null)
            {
                var mediaPlayer = MediaPlayer;
                if (mediaPlayer != null)
                {
                    FadeOutEnabled = false;
                    try
                    {
                        _ = FadeInAsync();

                        if (!RemoteRendering && RendererItems.Count == 1)
                        {
                            mediaPlayer.SetRenderer(RendererItems.First());
                            RemoteRendering = true;
                        }
                        else
                        {
                            var result = await this.FindAncestor<Page>()?.DisplayActionSheet(ResourceManager.GetString(nameof(Strings.CastTo)),
                                Cancel, Disconnect, RendererItems.Select(r => r.Name).OrderBy(r => r).ToArray());
                            if (result != null)
                            {
                                var rendererName = RendererItems.FirstOrDefault(r => r.Name == result);
                                if (rendererName != null)
                                {
                                    mediaPlayer.SetRenderer(rendererName);
                                    RemoteRendering = true;
                                }
                                else if (result == Disconnect)
                                {
                                    mediaPlayer.SetRenderer(null);
                                    RemoteRendering = false;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ShowErrorMessageBox(ex);
                    }
                    finally
                    {
                        FadeOutEnabled = true;
                    }
                }
            }
            _ = FadeInAsync();
        }

        private async void ClosedCaptionsSelectionButton_Clicked(object sender, EventArgs e)
        {
            await SelectTrackAsync(TrackType.Text, ResourceManager.GetString(nameof(Strings.ClosedCaptions)), m => m.Spu, (m, id) => m.SetSpu(id),
                true);
        }

        private void PlayPauseButton_Clicked(object sender, EventArgs e)
        {
            var mediaPlayer = MediaPlayer;
            if (mediaPlayer == null)
            {
                return;
            }

            _ = FadeInAsync();
            string playPauseState;
            switch (mediaPlayer.State)
            {
                case VLCState.Ended:
                    mediaPlayer.Stop();
                    goto case VLCState.Stopped;
                case VLCState.Error:
                case VLCState.Paused:
                case VLCState.Stopped:
                case VLCState.NothingSpecial:
                    playPauseState = PauseState;
                    break;
                default:
                    playPauseState = PlayState;
                    break;
            }
            VisualStateManager.GoToState((VisualElement)sender, playPauseState);
            if (playPauseState == PauseState)
            {
                mediaPlayer.Play();
            }
            else
            {
                mediaPlayer.Pause();
            }
        }

        private void StopButton_Clicked(object sender, EventArgs e)
        {
            MediaPlayer?.Stop();
        }

        private async void AspectRatioButton_Clicked(object sender, EventArgs e)
        {
            var mediaPlayer = MediaPlayer;
            if (mediaPlayer == null)
            {
                return;
            }

            try
            {
                var videoView = VideoView;
                if (videoView == null)
                {
                    throw new NullReferenceException($"The {nameof(VideoView)} property must be set in order to use the aspect ratio feature.");
                }

                MediaTrack? mediaTrack;
                try
                {
                    mediaTrack = mediaPlayer.Media?.Tracks?.FirstOrDefault(x => x.TrackType == TrackType.Video);
                }
                catch (Exception)
                {
                    mediaTrack = null;
                }
                if (mediaTrack == null || !mediaTrack.HasValue)
                {
                    return;
                }

                AspectRatio nextAspectRatio;

                if (CurrentAspectRatio == AspectRatio.Original)
                    nextAspectRatio = AspectRatio.BestFit;
                else
                    nextAspectRatio = ++CurrentAspectRatio;

                var scalingFactor = Device.Info.ScalingFactor;
                var displayW = videoView.Width * scalingFactor;
                var displayH = videoView.Height * scalingFactor;

                switch (nextAspectRatio)
                {
                    case AspectRatio.BestFit:
                        mediaPlayer.AspectRatio = string.Empty;
                        mediaPlayer.Scale = 0;
                        break;
                    case AspectRatio.FitScreen:
                    case AspectRatio.Fill:
                        var videoSwapped = mediaTrack.Value.Data.Video.Orientation == VideoOrientation.LeftBottom ||
                            mediaTrack.Value.Data.Video.Orientation == VideoOrientation.RightTop;
                        if(nextAspectRatio == AspectRatio.FitScreen)
                        {
                            var videoW = mediaTrack.Value.Data.Video.Width;
                            var videoH = mediaTrack.Value.Data.Video.Height;

                            if (videoSwapped)
                            {
                                var swap = videoW;
                                videoW = videoH;
                                videoH = swap;
                            }
                            if (mediaTrack.Value.Data.Video.SarNum != mediaTrack.Value.Data.Video.SarDen)
                                videoW = videoW * mediaTrack.Value.Data.Video.SarNum / mediaTrack.Value.Data.Video.SarDen;

                            var ar = videoW / (float)videoH;
                            var dar = displayW / (float)displayH;

                            float scale;
                            if (dar >= ar)
                                scale = (float)displayW / videoW; /* horizontal */
                            else
                                scale = (float)displayH / videoH; /* vertical */

                            mediaPlayer.Scale = scale;
                            mediaPlayer.AspectRatio = string.Empty;
                        }
                        else
                        {
                            mediaPlayer.Scale = 0;
                            mediaPlayer.AspectRatio = videoSwapped ? $"{displayH}:{displayW}" : $"{displayW}:{displayH}";
                        }
                        break;
                    case AspectRatio._16_9:
                        mediaPlayer.AspectRatio = "16:9";
                        mediaPlayer.Scale = 0;
                        break;
                    case AspectRatio._4_3:
                        mediaPlayer.AspectRatio = "4:3";
                        mediaPlayer.Scale = 0;
                        break;
                    case AspectRatio.Original:
                        mediaPlayer.AspectRatio = string.Empty;
                        mediaPlayer.Scale = 1;
                        break;
                }

                CurrentAspectRatio = nextAspectRatio;
                AspectRatioLabel.Text = AspectRatioLabels[CurrentAspectRatio];
                await AspectRatioLabel.FadeTo(1);
                await  AspectRatioLabel.FadeTo(0, 2000);
            }
            catch (Exception ex)
            {
                ShowErrorMessageBox(ex);
            }
        }

        private void RewindButton_Clicked(object sender, EventArgs e)
        {
            var mediaPlayer = MediaPlayer;
            if (mediaPlayer == null)
            {
                return;
            }

            mediaPlayer.Time -= SEEK_OFFSET;
        }

        private void SeekButton_Clicked(object sender, EventArgs e)
        {
            var mediaPlayer = MediaPlayer;
            if (mediaPlayer == null)
            {
                return;
            }

            mediaPlayer.Time += SEEK_OFFSET;
        }

        private Button SetClickEventHandler(string name, EventHandler eventHandler, bool fadeIn = false)
        {
            var button = this.FindChild<Button>(name);
            if (button != null)
            {
                button.Clicked += (sender, e) => OnButtonClicked(sender, e, eventHandler, fadeIn);
            }
            return button;
        }

        private void OnButtonClicked(object sender, EventArgs e, EventHandler eventHandler, bool fadeIn = false)
        {
            if (fadeIn)
            {
                _ = FadeInAsync();
            }

            try
            {
                eventHandler?.Invoke(sender, e);
            }
            catch (Exception ex)
            {
                ShowErrorMessageBox(ex);
            }
        }

        private void OnMediaPlayerChanged(LibVLCSharp.Shared.MediaPlayer oldMediaPlayer, LibVLCSharp.Shared.MediaPlayer newMediaPlayer)
        {
            if (oldMediaPlayer != null)
            {
                oldMediaPlayer.Buffering -= MediaPlayer_Buffering;
                oldMediaPlayer.EncounteredError -= MediaPlayer_EncounteredError;
                oldMediaPlayer.EndReached -= MediaPlayer_EndReached;
                oldMediaPlayer.ESAdded -= MediaPlayer_TracksChanged;
                oldMediaPlayer.ESDeleted -= MediaPlayer_TracksChanged;
                oldMediaPlayer.LengthChanged -= MediaPlayer_LengthChanged;
                oldMediaPlayer.MediaChanged -= MediaPlayer_MediaChanged;
                oldMediaPlayer.NothingSpecial -= MediaPlayer_NothingSpecial;
                oldMediaPlayer.PausableChanged -= MediaPlayer_PausableChanged;
                oldMediaPlayer.Paused -= MediaPlayer_Paused;
                oldMediaPlayer.Playing -= MediaPlayer_Playing;
                oldMediaPlayer.PositionChanged -= MediaPlayer_PositionChanged;
                oldMediaPlayer.SeekableChanged -= MediaPlayer_SeekableChanged;
                oldMediaPlayer.Stopped -= MediaPlayer_Stopped;
            }

            if (newMediaPlayer != null)
            {
                newMediaPlayer.Buffering += MediaPlayer_Buffering;
                newMediaPlayer.EncounteredError += MediaPlayer_EncounteredError;
                newMediaPlayer.ESAdded += MediaPlayer_TracksChanged;
                newMediaPlayer.ESDeleted += MediaPlayer_TracksChanged;
                newMediaPlayer.EndReached += MediaPlayer_EndReached;
                newMediaPlayer.LengthChanged += MediaPlayer_LengthChanged;
                newMediaPlayer.MediaChanged += MediaPlayer_MediaChanged;
                newMediaPlayer.NothingSpecial += MediaPlayer_NothingSpecial;
                newMediaPlayer.PausableChanged += MediaPlayer_PausableChanged;
                newMediaPlayer.Paused += MediaPlayer_Paused;
                newMediaPlayer.Playing += MediaPlayer_Playing;
                newMediaPlayer.PositionChanged += MediaPlayer_PositionChanged;
                newMediaPlayer.SeekableChanged += MediaPlayer_SeekableChanged;
                newMediaPlayer.Stopped += MediaPlayer_Stopped;
            }

            Reset();
        }

        private IEnumerable<MediaTrack> GetMediaTracks(LibVLCSharp.Shared.MediaPlayer mediaPlayer, TrackType trackType)
        {
            try
            {
                return mediaPlayer?.Media?.Tracks.Where(t => t.TrackType == trackType);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string GetTrackName(string trackName, int trackId, int currentTrackId)
        {
            return trackId == currentTrackId ? $"{trackName} *" : trackName;
        }

        private string GetTrackName(MediaTrack mediaTrack, int currentTrackId, int index)
        {
            var trackName = mediaTrack.Description ?? mediaTrack.Language ?? string.Format(ResourceManager.GetString(nameof(Strings.Track)), index);
            return GetTrackName(trackName, mediaTrack.Id, currentTrackId);
        }

        private async Task SelectTrackAsync(TrackType trackType, string popupTitle, Func<LibVLCSharp.Shared.MediaPlayer, int> getCurrentTrackId,
            Action<LibVLCSharp.Shared.MediaPlayer, int> setCurrentTrackId, bool addDeactivateRow = false)
        {
            var mediaPlayer = MediaPlayer;
            var mediaTracks = GetMediaTracks(mediaPlayer, trackType);
            if (mediaTracks == null)
            {
                return;
            }

            try
            {
                var currentTrackId = getCurrentTrackId(mediaPlayer);
                var index = 0;
                IEnumerable<string> mediaTracksNames = mediaTracks.Select(t =>
                    {
                        index += 1;
                        return GetTrackName(t, currentTrackId, index);
                    }).OrderBy(n => n);
                if (addDeactivateRow)
                {
                    mediaTracksNames = new[] { GetTrackName(ResourceManager.GetString(nameof(Strings.Disable)), -1, currentTrackId) }
                    .Union(mediaTracksNames);
                }

                var mediaTrack = await this.FindAncestor<Page>()?.DisplayActionSheet(popupTitle, null, null, mediaTracksNames.ToArray());
                if (mediaTrack != null)
                {
                    var found = false;
                    index = 0;
                    foreach (var mt in mediaTracks)
                    {
                        index += 1;
                        if (GetTrackName(mt, currentTrackId, index) == mediaTrack)
                        {
                            found = true;
                            setCurrentTrackId(mediaPlayer, mt.Id);
                            break;
                        }
                    }
                    if (!found)
                    {
                        setCurrentTrackId(mediaPlayer, -1);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessageBox(ex);
            }
        }

        private void UpdateTracksSelectionButtonAvailability(Button tracksSelectionButton, string state)
        {
            if (tracksSelectionButton != null)
            {
                Device.BeginInvokeOnMainThread(() => VisualStateManager.GoToState(tracksSelectionButton, state));
            }
        }

        private void UpdateTracksSelectionAvailability(LibVLCSharp.Shared.MediaPlayer mediaPlayer, Button tracksSelectionButton,
            bool isTracksSelectionButtonVisible, Func<LibVLCSharp.Shared.MediaPlayer, IEnumerable<TrackDescription>> getTrackDescriptions,
            string availableState, string unavailableState, int count)
        {
            if (tracksSelectionButton != null)
            {
                UpdateTracksSelectionButtonAvailability(tracksSelectionButton, isTracksSelectionButtonVisible &&
                    mediaPlayer != null && getTrackDescriptions(mediaPlayer).Where(t => t.Id != -1).Count() >= count ?
                    availableState : unavailableState);
            }
        }

        private void UpdateAudioTracksSelectionAvailability()
        {
            UpdateTracksSelectionAvailability(MediaPlayer, AudioTracksSelectionButton, IsAudioTracksSelectionButtonVisible,
                m => m.AudioTrackDescription, AudioSelectionAvailableState, AudioSelectionUnavailableState, 2);
        }

        private void UpdateClosedCaptionsTracksSelectionAvailability()
        {
            UpdateTracksSelectionAvailability(MediaPlayer, ClosedCaptionsSelectionButton, IsClosedCaptionsSelectionButtonVisible,
                m => m.SpuDescription, ClosedCaptionsSelectionAvailableState, ClosedCaptionsSelectionUnavailableState, 1);
        }

        private void ShowError(string errorMessage)
        {
            Device.BeginInvokeOnMainThread(() => ErrorMessage = errorMessage);
        }

        private void Reset()
        {
            UpdateState();
            UpdateAudioTracksSelectionAvailability();
            UpdateClosedCaptionsTracksSelectionAvailability();
            UpdatePosition();
            UpdatePauseAvailability();
            UpdateSeekAvailability();
            UpdateErrorMessage();
        }

        private void UpdateErrorMessage(VLCState? state = null)
        {
            try
            {
                state = state ?? MediaPlayer?.State;
                if (state == VLCState.Error || state == VLCState.Ended)
                {
                    ShowError(LibVLC?.LastLibVLCError);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessageBox(ex);
            }
        }

        private void UpdateKeepScreenOn(bool? keepScreenOn = null)
        {
            var letScreenOn = keepScreenOn ?? KeepScreenOn;
            if (!letScreenOn)
            {
                return;
            }

            var powerManager = PowerManager;
            if (powerManager == null)
            {
                return;
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var state = MediaPlayer?.State;
                    letScreenOn = state == VLCState.Opening || state == VLCState.Buffering || state == VLCState.Playing;
                    if (powerManager.KeepScreenOn != letScreenOn)
                    {
                        powerManager.KeepScreenOn = letScreenOn;
                    }
                }
                catch (Exception)
                {
                }
            });
        }

        private void UpdatePauseAvailability(bool? canPause = null)
        {
            var playPauseButton = PlayPauseButton;
            if (playPauseButton != null)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    var mediaPlayer = MediaPlayer;
                    var state = mediaPlayer?.State;
                    VisualStateManager.GoToState(playPauseButton, IsPlayPauseButtonVisible && mediaPlayer?.Media != null &&
                        ((canPause ?? mediaPlayer?.CanPause) != false ||
                        state != VLCState.Opening && state != VLCState.Playing && state != VLCState.Buffering) ?
                        PauseAvailableState : PauseUnavailableState);
                });
            }
        }

        private void UpdateSeekAvailability(bool? canSeek = null)
        {
            var seekBar = SeekBar;
            if (seekBar != null)
            {
                Device.BeginInvokeOnMainThread(() => VisualStateManager.GoToState(seekBar, IsSeekEnabled &&
                    (canSeek ?? MediaPlayer?.IsSeekable == true) ? SeekAvailableState : SeekUnavailableState));
            }
        }

        private void UpdateCastAvailability()
        {
            var castButton = CastButton;
            if (castButton != null)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    VisualStateManager.GoToState(castButton, IsCastButtonVisible && LibVLC != null && EnableRendererDiscovery && RendererItems.Any()
                        ? CastAvailableState : CastUnavailableState);
                });
            }
        }

        private void SeekBar_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            _ = FadeInAsync();

            UpdateTime();
            UpdateMediaPlayerPosition();
        }

        private void UpdateMediaPlayerPosition()
        {
            try
            {
                var mediaPlayer = MediaPlayer;
                if (mediaPlayer != null)
                {
                    mediaPlayer.Position = (float)(SeekBar.Value / SeekBar.Maximum);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessageBox(ex);
            }
        }

        private void UpdatePosition(float? position = null)
        {
            try
            {
                var mediaPlayer = MediaPlayer;
                var pos = position ?? mediaPlayer?.Position ?? 0;
                var timeSpanPosition = TimeSpan.FromMilliseconds((mediaPlayer?.Length ?? 0) * pos);
                var elapsedTime = (timeSpanPosition - Position).TotalMilliseconds;
                if (position != null && elapsedTime > 0 && elapsedTime < 750)
                {
                    return;
                }

                UpdateSeekBar(timeSpanPosition, pos);
            }
            catch (Exception ex)
            {
                ShowErrorMessageBox(ex);
            }
        }

        private void UpdateTime(double? position = null)
        {
            if (RemainingTimeLabel == null || ElapsedTimeLabel == null)
            {
                return;
            }

            try
            {
                var mediaPlayer = MediaPlayer;
                var state = mediaPlayer?.State;
                var length = mediaPlayer == null || state == VLCState.Ended || state == VLCState.Error || state == VLCState.NothingSpecial ||
                    state == VLCState.Stopped ? 0 : mediaPlayer.Length;
                var time = position ?? (SeekBar.Value * length / SeekBar.Maximum);
                var timeRemaining = $"- {TimeSpan.FromMilliseconds(length - time).ToShortString()}";
                var timeElapsed = TimeSpan.FromMilliseconds(time).ToShortString();
                if (RemainingTimeLabel.Text != timeRemaining)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        RemainingTimeLabel.Text = timeRemaining;
                        ElapsedTimeLabel.Text = timeElapsed;
                    });
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessageBox(ex);
            }
        }

        private void UpdateSeekBar(TimeSpan timeSpan, float position = 0)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                Position = timeSpan;
                var seekBar = SeekBar;
                if (seekBar != null)
                {
                    seekBar.ValueChanged -= SeekBar_ValueChanged;
                    seekBar.Value = position * seekBar.Maximum;
                    seekBar.ValueChanged += SeekBar_ValueChanged;
                }
            });
            UpdateTime(timeSpan.TotalMilliseconds);
        }

        private void UpdateState(VLCState? state = null)
        {
            state = state ?? MediaPlayer?.State ?? VLCState.NothingSpecial;
            string playPauseState;
            switch (state)
            {
                case VLCState.Error:
                    UpdateErrorMessage(state);
                    goto case VLCState.Stopped;
                case VLCState.Stopped:
                case VLCState.Ended:
                case VLCState.NothingSpecial:
                    UpdateSeekBar(TimeSpan.Zero);
                    UpdatePauseAvailability(true);
                    UpdateSeekAvailability(false);
                    UpdateTracksSelectionButtonAvailability(AudioTracksSelectionButton, AudioSelectionUnavailableState);
                    UpdateTracksSelectionButtonAvailability(ClosedCaptionsSelectionButton, ClosedCaptionsSelectionUnavailableState);
                    goto case VLCState.Paused;
                case VLCState.Paused:
                    playPauseState = PlayState;
                    break;
                default:
                    ShowError(null);
                    playPauseState = PauseState;
                    break;
            }

            Device.BeginInvokeOnMainThread(() =>
            {
                _ = FadeInAsync();
                BufferingProgress = 0;
                var playPauseButton = PlayPauseButton;
                if (playPauseButton != null)
                {
                    VisualStateManager.GoToState(playPauseButton, playPauseState);
                }
            });
            UpdateKeepScreenOn();
        }

        private void FadeOut()
        {
            Device.BeginInvokeOnMainThread(async () =>
            {
                if (await ControlsPanel.FadeTo(0, 1000) && ControlsPanel.Opacity == 0)
                {
                    ControlsPanel.IsVisible = false;
                }
            });

            Device.BeginInvokeOnMainThread(() =>
            {
                var systemUI = SystemUI;
                if (systemUI != null)
                {
                    systemUI.HideSystemUI();
                }
            });
        }

        ObservableCollection<RendererItem> RendererItems = new ObservableCollection<RendererItem>();
        RendererDiscoverer RendererDiscoverer;

        private void ClearRenderer()
        {
            if(RendererDiscoverer != null)
            {
                RendererDiscoverer.Stop();
                RendererDiscoverer.ItemAdded -= RendererDiscoverer_ItemAdded;
                RendererDiscoverer.ItemDeleted -= RendererDiscoverer_ItemDeleted;
                RendererDiscoverer.Dispose();
                RendererDiscoverer = null;
            }
        }

        private void FindRenderers()
        {
            if (LibVLC == null)
                return;

            if (!EnableRendererDiscovery)
                return;

            var rendererDiscoverer = new RendererDiscoverer(LibVLC);
            rendererDiscoverer.ItemAdded += RendererDiscoverer_ItemAdded;
            rendererDiscoverer.ItemDeleted += RendererDiscoverer_ItemDeleted;
            rendererDiscoverer.Start();
        }

        private void RendererDiscoverer_ItemDeleted(object sender, RendererDiscovererItemDeletedEventArgs e) => RendererItems.Remove(e.RendererItem);

        private void RendererDiscoverer_ItemAdded(object sender, RendererDiscovererItemAddedEventArgs e) => RendererItems.Add(e.RendererItem);

        /// <summary>
        /// Show an error message box.
        /// </summary>
        /// <param name="ex">The exception to show.</param>
        protected virtual void ShowErrorMessageBox(Exception ex)
        {
            var error = ResourceManager.GetString(nameof(Strings.Error));
            Device.BeginInvokeOnMainThread(() => this.FindAncestor<Page>().DisplayAlert(error, ex?.GetBaseException().Message ?? error,
                ResourceManager.GetString(nameof(Strings.OK))));
        }

        /// <summary>
        /// Controls fade in.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task FadeInAsync()
        {
            var controlsPanel = ControlsPanel;
            if (controlsPanel == null)
            {
                return;
            }

            FadeOutTimer.Change(TimeSpan.FromMilliseconds(-1), TimeSpan.FromMilliseconds(-1));
            controlsPanel.IsVisible = true;
            if (controlsPanel.Opacity != 1)
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    var systemUI = SystemUI;
                    if (systemUI != null)
                    {
                        systemUI.ShowSystemUI();
                    }
                });
                await controlsPanel.FadeTo(1);
            }
            if (FadeOutEnabled && ShowAndHideAutomatically && MediaPlayer?.State == VLCState.Playing)
            {
                FadeOutTimer.Change(TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(-1));
            }
        }
    }

    internal enum AspectRatio
    {
        BestFit,
        FitScreen,
        Fill,
        _16_9,
        _4_3,
        Original
    }
}
