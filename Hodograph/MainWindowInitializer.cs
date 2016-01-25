using OxyPlot;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Hodograph
{
    public partial class MainWindow
    {
        #region Properties

        ObservableCollection<DataPoint> phasePoints;
        public ObservableCollection<DataPoint> PhasePoints
        {
            get { return phasePoints; }
        }

        ObservableCollection<DataPoint> positionPoints;
        public ObservableCollection<DataPoint> PositionPoints
        {
            get { return positionPoints; }
        }

        ObservableCollection<DataPoint> currentPhasePoints;
        public ObservableCollection<DataPoint> CurrentPhasePoints
        {
            get { return currentPhasePoints; }
        }

        ObservableCollection<DataPoint> velocityPoints;
        public ObservableCollection<DataPoint> VelocityPoints
        {
            get { return velocityPoints; }
        }

        ObservableCollection<DataPoint> accelerationPoints;
        public ObservableCollection<DataPoint> AccelerationPoints
        {
            get { return accelerationPoints; }
        }

        private double animationSpeed;
        public double AnimationSpeed
        {
            get { return animationSpeed; }
            set
            {
                if (value != animationSpeed)
                {
                    animationSpeed = value;
                    OnPropertyChanged("AnimationSpeed");
                }
            }
        }

        private double epsilon0;
        public double Epsilon0
        {
            get { return epsilon0; }
            set
            {
                if (value != epsilon0)
                {
                    epsilon0 = value;
                    OnPropertyChanged("Epsilon0");
                }
            }
        }

        private double l;
        public double L
        {
            get { return l; }
            set
            {
                if (value != l)
                {
                    l = value;
                    OnPropertyChanged("L");
                    if (R > L) L = R;
                    rimBlockDistance = Math.Sqrt(L * L - R * R);
                    rimCenter = new Point(CanvasHalfWidth - rimBlockDistance * 2 / 3, CanvasHalfHeight);
                    if(!animationStarted)
                        SetupSceneObjects();
                }
            }
        }

        private double lCurrent;
        public double LCurrent
        {
            get { return lCurrent; }
            set
            {
                if (value != lCurrent)
                {
                    lCurrent = value;
                    OnPropertyChanged("LCurrent");
                }
            }
        }

        private double r;
        public double R
        {
            get { return r; }
            set
            {
                if (value != r)
                {
                    r = value;
                    OnPropertyChanged("R");
                    if (R > L) L = R;
                    rimBlockDistance = Math.Sqrt(L * L - R * R);
                    rimCenter = new Point(CanvasHalfWidth - rimBlockDistance * 2 / 3, CanvasHalfHeight);
                    if (!animationStarted)
                        SetupSceneObjects();
                }
            }
        }

        private double alpha;
        public double Alpha
        {
            get { return alpha; }
            set
            {
                if (value != alpha)
                {
                    alpha = value;
                    OnPropertyChanged("Alpha");
                }
            }
        }

        private double omega;
        public double Omega
        {
            get { return omega; }
            set
            {
                if (value != omega)
                {
                    omega = value;
                    OnPropertyChanged("Omega");
                }
            }
        }

        private double time;
        public double Time
        {
            get { return time; }
            set
            {
                if (value != time)
                {
                    time = value;
                    OnPropertyChanged("Time");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Fields

        Random rnd = new Random(DateTime.Now.Millisecond);
        System.Windows.Threading.DispatcherTimer dispatcherTimer;
        private const double Epsilon = 0.0001;
        private const double CanvasWidth = 700;
        private const double CanvasHalfWidth = 350;
        private const double CanvasHeight = 350;
        private const double CanvasHalfHeight = 175;
        private bool animationStarted = false;
        private DateTime startTime;
        private DateTime stopTime;
        private TimeSpan timeDelay;
        private bool[] buttonsFlags;
        private Ellipse rim;
        private Line joint;
        private Point rimCenter;
        private double rimBlockDistance;
        private Ellipse rimPoint;
        private Ellipse blockPoint;
        private Rectangle block;
        private Line axisX;
        private Line axisY;
        private Line verticalSpoke;
        private Line horizontalSpoke;
        private const double StartAlpha = Math.PI / 2;
        private double currentX;
        private double nextX;
        private double nextNextX;
        private Point positionOnRim;
        private Point positionOnBlock;
        private double currentV;
        private double currentA;
        private double lastTime;
        private double deltaTime;
        private GaussianRandom gaussianRandom;
        private const int MaxPointsCount = 200;

        #endregion

        #region Methods

        /// <summary>
        /// Window initialization
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Initialize();
            InitializeScene();
        }

        private void Initialize()
        {
            gaussianRandom = new GaussianRandom(rnd);
            phasePoints = new ObservableCollection<DataPoint>();
            currentPhasePoints = new ObservableCollection<DataPoint>();
            positionPoints = new ObservableCollection<DataPoint>();
            velocityPoints = new ObservableCollection<DataPoint>();
            accelerationPoints = new ObservableCollection<DataPoint>();
            animationSpeed = 0;
            epsilon0 = 0.05;
            alpha = StartAlpha;
            omega = 2;
            r = 80;
            lCurrent = l = 300;
            rimBlockDistance = Math.Sqrt(L * L - R * R);
            rimCenter = new Point(CanvasHalfWidth - rimBlockDistance * 2 / 3, CanvasHalfHeight);

            currentX = rimBlockDistance;
            currentV = 0.0;
            currentA = 0.0;
            lastTime = 0.0;
            deltaTime = 0;

            dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimerTick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            buttonsFlags = new bool[3] { false, false, false };
        }

        private void InitializeScene()
        {
            PositionPlot.DefaultColors = new Color[1]{ Colors.DodgerBlue };
            VelocityPlot.DefaultColors = new Color[1] { Colors.DodgerBlue };
            AccelerationPlot.DefaultColors = new Color[1] { Colors.DodgerBlue };
            PhasePlot.DefaultColors = new Color[1] { Colors.DodgerBlue };


            axisX = new Line();
            axisX.StrokeThickness = 1;
            axisX.Stroke = Brushes.Black;
            SimulationCanvas.Children.Add(axisX);

            axisY = new Line();
            axisY.StrokeThickness = 1;
            axisY.Stroke = Brushes.Black;
            SimulationCanvas.Children.Add(axisY);

            rim = new Ellipse();
            rim.Stroke = Brushes.Blue;
            rim.StrokeThickness = 3;
            SimulationCanvas.Children.Add(rim);

            verticalSpoke = new Line();
            verticalSpoke.Stroke = Brushes.Blue;
            verticalSpoke.StrokeThickness = 2;
            SimulationCanvas.Children.Add(verticalSpoke);

            horizontalSpoke = new Line();
            horizontalSpoke.Stroke = Brushes.Blue;
            horizontalSpoke.StrokeThickness = 2;
            SimulationCanvas.Children.Add(horizontalSpoke);

            rimPoint = new Ellipse();
            rimPoint.Width = rimPoint.Height = 8;
            rimPoint.Fill = Brushes.Blue;
            SimulationCanvas.Children.Add(rimPoint);

            blockPoint = new Ellipse();
            blockPoint.Width = blockPoint.Height = 8;
            blockPoint.Fill = Brushes.Blue;
            SimulationCanvas.Children.Add(blockPoint);

            joint = new Line();
            joint.Stroke = Brushes.OrangeRed;
            joint.StrokeThickness = 3;
            SimulationCanvas.Children.Add(joint);

            block = new Rectangle();
            block.Stroke = Brushes.Blue;
            block.StrokeThickness = 3;
            SimulationCanvas.Children.Add(block);

            SetupSceneObjects();
        }
        #endregion
    }
}
