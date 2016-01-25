using OxyPlot;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;

namespace Hodograph
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        private void DispatcherTimerTick(object sender, EventArgs e)
        {
            var currentTime = DateTime.Now;
            var timeDiff = currentTime - startTime - timeDelay;
            Time = timeDiff.TotalSeconds;
            deltaTime = Time - lastTime;
            lastTime = Time;

            calculatePositions();

            positionPoints.Add(new DataPoint(Time, currentX));

            //forward derivative
            currentV = (nextX - currentX) / deltaTime;
            velocityPoints.Add(new DataPoint(Time, currentV));
            phasePoints.Add(new DataPoint(currentX, currentV));

            //forward derivative
            currentA = (nextNextX - 2 * nextX + currentX) / (deltaTime * deltaTime);
            accelerationPoints.Add(new DataPoint(Time, currentA));
            reduceGraphPoints();
        }


        private void adjustAngle(ref double Alpha)
        {
            if (Alpha > Math.PI * 2) 
                Alpha -= Math.PI * 2;
        }

        private void calculateSinglePosition(double angle)
        {
            adjustAngle(ref angle);
            if (Epsilon0 > 0)
                LCurrent = L + gaussianRandom.GetNormalGaussian(0, Epsilon0);
            else
                LCurrent = L;
            positionOnRim = new Point(Math.Cos(angle) * R, Math.Sin(angle) * R);
            positionOnBlock = new Point(Math.Sqrt(LCurrent * LCurrent - positionOnRim.Y * positionOnRim.Y) + positionOnRim.X, 0);
            if (double.IsNaN(positionOnBlock.X)) positionOnBlock.X = 0;
        }

        private void calculateCurrentPosition()
        {
            var angle = Alpha + Omega * deltaTime;
            calculateSinglePosition(angle);
            UpdateSceneObjects(positionOnRim, positionOnBlock);
            currentX = positionOnBlock.X;
        }

        private void calculateNextPosition()
        {
            var angle = Alpha + 2 * Omega * deltaTime;
            calculateSinglePosition(angle);
            nextX = positionOnBlock.X;
        }

        private void calculateNextNextPosition()
        {
            var angle = Alpha + 3 * Omega * deltaTime;
            calculateSinglePosition(angle);
            nextNextX = positionOnBlock.X;
        }

        private void calculatePositions()
        {
            calculateCurrentPosition();
            calculateNextPosition();
            calculateNextNextPosition();
            //update angle
            Alpha = Alpha + Omega * deltaTime;
        }

        private void reduceGraphPoints()
        {
            if (positionPoints.Count > MaxPointsCount)
                positionPoints.RemoveAt(0);
            if (velocityPoints.Count > MaxPointsCount)
                velocityPoints.RemoveAt(0);
            if (accelerationPoints.Count > MaxPointsCount)
                accelerationPoints.RemoveAt(0);
            if (phasePoints.Count > MaxPointsCount)
                phasePoints.RemoveAt(0);
        }

        private double Norm(Vector3D v)
        {
            return Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (buttonsFlags[0]) return;
            for (int i = 0; i < buttonsFlags.Length; i++)
                buttonsFlags[i] = false;
            buttonsFlags[0] = true;

            if (!animationStarted)
            {
                //AnimationSpeedSlider.IsEnabled = false;
                animationStarted = true;
                startTime = DateTime.Now;
                dispatcherTimer.Start();
                return;
            }
            timeDelay += DateTime.Now - stopTime;
            dispatcherTimer.Start();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (buttonsFlags[1]) return;
            for (int i = 0; i < buttonsFlags.Length; i++)
                buttonsFlags[i] = false;
            buttonsFlags[1] = true;

            dispatcherTimer.Stop();
            stopTime = DateTime.Now;
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (buttonsFlags[2]) return;
            for (int i = 0; i < buttonsFlags.Length; i++)
                buttonsFlags[i] = false;
            buttonsFlags[2] = true;

            dispatcherTimer.Stop();
            animationStarted = false;
            //AnimationSpeedSlider.IsEnabled = true;
            AnimationSpeed = 0;
            ResetParameters();
            SetupSceneObjects();

            positionPoints.Clear();
            velocityPoints.Clear();
            accelerationPoints.Clear();
            phasePoints.Clear();
            //currentPhasePoints.Clear();

            ResetAxes();
        }

        private void ResetAxes()
        {
            foreach (var axis in PositionPlot.Axes)
                axis.InternalAxis.Reset();
            foreach (var axis in VelocityPlot.Axes)
                axis.InternalAxis.Reset();
            foreach (var axis in AccelerationPlot.Axes)
                axis.InternalAxis.Reset();
            foreach (var axis in PhasePlot.Axes)
                axis.InternalAxis.Reset();
        }

        private void ResetParameters()
        {
            timeDelay = new TimeSpan();
            Time = 0;
            LCurrent = L;
            Alpha = StartAlpha;
            currentX = rimBlockDistance;
            lastTime = 0;
            deltaTime = 0;
        }

        private void UpdateSceneObjects(Point positionOnRim, Point positionOnBlock)
        {
            axisY.X1 = rimCenter.X;
            axisY.X2 = rimCenter.X;

            rim.Width = rim.Height = R * 2;
            Canvas.SetLeft(rim, rimCenter.X - R);
            Canvas.SetTop(rim, rimCenter.Y - R);

            verticalSpoke.X1 = rimCenter.X + positionOnRim.X;
            verticalSpoke.Y1 = rimCenter.Y - positionOnRim.Y;
            verticalSpoke.X2 = rimCenter.X - positionOnRim.X;
            verticalSpoke.Y2 = rimCenter.Y + positionOnRim.Y;

            horizontalSpoke.X1 = rimCenter.X - positionOnRim.Y;
            horizontalSpoke.Y1 = rimCenter.Y - positionOnRim.X;
            horizontalSpoke.X2 = rimCenter.X + positionOnRim.Y;
            horizontalSpoke.Y2 = rimCenter.Y + positionOnRim.X;

            Canvas.SetLeft(rimPoint, rimCenter.X + positionOnRim.X - 4);
            Canvas.SetTop(rimPoint, rimCenter.Y - positionOnRim.Y - 4);

            Canvas.SetLeft(blockPoint, rimCenter.X + positionOnBlock.X - 4);
            Canvas.SetTop(blockPoint, rimCenter.Y - 4);

            joint.X1 = rimCenter.X + positionOnRim.X;
            joint.Y1 = rimCenter.Y - positionOnRim.Y;
            joint.X2 = rimCenter.X + positionOnBlock.X;
            joint.Y2 = rimCenter.Y;

            block.Width = R * 1.5;
            block.Height = R * 1;
            Canvas.SetLeft(block, rimCenter.X + positionOnBlock.X);
            Canvas.SetTop(block, rimCenter.Y - block.Height / 2);
        }

        private void SetupSceneObjects()
        {
            axisX.X1 = 0;
            axisX.Y1 = rimCenter.Y;
            axisX.X2 = CanvasWidth;
            axisX.Y2 = rimCenter.Y;

            axisY.X1 = rimCenter.X;
            axisY.Y1 = -1000;
            axisY.X2 = rimCenter.X;
            axisY.Y2 = 1000;//CanvasHeight;

            rim.Width = rim.Height = R * 2;
            Canvas.SetLeft(rim, rimCenter.X - R);
            Canvas.SetTop(rim, rimCenter.Y - R);

            verticalSpoke.X1 = rimCenter.X;
            verticalSpoke.Y1 = rimCenter.Y - R;
            verticalSpoke.X2 = rimCenter.X;
            verticalSpoke.Y2 = rimCenter.Y + R;

            horizontalSpoke.X1 = rimCenter.X - R;
            horizontalSpoke.Y1 = rimCenter.Y;
            horizontalSpoke.X2 = rimCenter.X + R;
            horizontalSpoke.Y2 = rimCenter.Y;

            Canvas.SetLeft(rimPoint, rimCenter.X - 4);
            Canvas.SetTop(rimPoint, rimCenter.Y - R - 4);

            Canvas.SetLeft(blockPoint, rimCenter.X + rimBlockDistance - 4);
            Canvas.SetTop(blockPoint, rimCenter.Y - 4);

            joint.X1 = rimCenter.X;
            joint.Y1 = rimCenter.Y - R;
            joint.X2 = rimCenter.X + rimBlockDistance;
            joint.Y2 = rimCenter.Y;

            block.Width = R * 1.5;
            block.Height = R * 1;
            Canvas.SetLeft(block, rimCenter.X + rimBlockDistance);
            Canvas.SetTop(block, rimCenter.Y - block.Height / 2);
        }
    }
}
