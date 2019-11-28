using MazeRunnerWPF.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MazeRunnerWPF.MazeGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MazeGui : Page, IGuiPage
    {
        public const int THREAD_SLEEP = 1000 / 90;  // 90 fps for removing stuttering
        private MazeGuiBuilder mazeBuilder;
        private (int x, int y) currentLocation;
        private CardinalDirs currentDir;

        public MazeGui()
        {
            acceptInput = true;
            InitializeComponent();
            mazeBuilder = new MazeGuiBuilder(3);
            currentLocation = mazeBuilder.GetEntranceLoc();
            CurrentAngle = targetAngle = GetLookRotation();
            currentDir = CardinalDirs.NORTH;
            BuildCurrentLocation();

            //GuiMediator.Instance.SetMazeGui(this);
        }

        private void MoveRoomsAuto()
        {
            switch (currentDir)
            {
                case CardinalDirs.NORTH:
                    currentLocation.y--;
                    break;
                case CardinalDirs.SOUTH:
                    currentLocation.y++;
                    break;
                case CardinalDirs.EAST:
                    currentLocation.x++;
                    break;
                case CardinalDirs.WEST:
                    currentLocation.x--;
                    break;
            }
        }

        private delegate void TriggerBuildCurrentLocation();
        private void BuildCurrentLocation()
        {
            UpdateIfCanMove();
            mazeBuilder.BuildRoomTextures(
                currentLocation.x,
                currentLocation.y,
                ref matDiffuseNorth,
                ref matDiffuseSouth,
                ref matDiffuseWest,
                ref matDiffuseEast,
                ref matDiffuseFloor,
                ref matDiffuseCeiling
            );
        }



        private bool acceptInput;

        private void btnTurnLeft_Click(object sender, RoutedEventArgs e) { TurnLeft(); }
        private void btnTurnRight_Click(object sender, RoutedEventArgs e) { TurnRight(); }
        private void btnAction_Click(object sender, RoutedEventArgs e) { DoAction(); }

        public void OnShown(object passingObj)
        {
            Console.WriteLine("Added keydown events");
            var window = Window.GetWindow(this);
            window.KeyDown += Page_KeyDown;

            if (isWaitingOnQuestion)
            {
                isWaitingOnQuestion = false;

                bool answeredCorrectly = (bool)passingObj;
                if (answeredCorrectly)
                {
                    MoveToZ(1);
                }
                else
                {
                    acceptInput = true;
                }
            }
        }

        public object OnDisappeared()
        {
            Console.WriteLine("Removed keydown events");
            var window = Window.GetWindow(this);
            window.KeyDown -= Page_KeyDown;
            return null;
        }

        private void Page_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Left) TurnLeft();
            else if (e.Key == Key.Right) TurnRight();
            else if (e.Key == Key.Up) DoAction();
        }



        private void UpdateIfCanMove()
        {
            btnAction.IsEnabled = CanMove();
        }

        private bool CanMove()
        {
            return !mazeBuilder.IsWall(currentLocation.x, currentLocation.y, currentDir);
        }

        private void TurnLeft()
        {
            if (!acceptInput) return;
            acceptInput = false;

            currentDir = CardinalDirsUtils.TurnLeft(currentDir);
            UpdateIfCanMove();
            Turn(-90);
        }

        private void TurnRight()
        {
            if (!acceptInput) return;
            acceptInput = false;

            currentDir = CardinalDirsUtils.TurnRight(currentDir);
            UpdateIfCanMove();
            Turn(90);
        }

        private bool isWaitingOnQuestion;
        private void DoAction()
        {
            isWaitingOnQuestion = false;
            if (CanMove())
            {
                if (!acceptInput) return;
                acceptInput = false;

                isWaitingOnQuestion = true;
                GuiMediator.Instance.ShowQuestionGui();
            }
        }


        private double targetAngle;
        private void Turn(double angle)
        {
            targetAngle += angle;
            CurrentAngle = GetLookRotation();
            new Thread(new ThreadStart(TurnAnimateAsync)).Start();
        }

        private delegate void UpdateSetLookRotation(double angle);
        private void SetLookRotation(double angle)
        {
            lookRotation.Angle = angle;
        }

        private double GetLookRotation()
        {
            return lookRotation.Angle;
        }

        private double CurrentAngle;
        private void TurnAnimateAsync()
        {
            int ticks = 30;
            double turnDelta = (targetAngle - CurrentAngle) / ticks;
            for (int i = 0; i < ticks; i++)
            {
                Thread.Sleep(THREAD_SLEEP);
                CurrentAngle += turnDelta;
                Dispatcher.Invoke(
                    new UpdateSetLookRotation(this.SetLookRotation),
                    new object[] { CurrentAngle }
                );
            }

            acceptInput = true;
        }


        private double targetZ, currentZ;
        private void MoveToZ(double newZ)
        {
            targetZ = newZ;
            currentZ = camMain.Position.Z;
            new Thread(new ThreadStart(MoveAnimateAsync)).Start();
        }

        private void MoveAnimateAsync()
        {
            int ticks = 30;
            double moveDelta = (targetZ - currentZ) / ticks;
            for (int i = 0; i < ticks; i++)
            {
                Thread.Sleep(THREAD_SLEEP);
                currentZ += moveDelta;
                Dispatcher.Invoke(
                    new UpdateSetZPos(this.SetZPos),
                    new object[] { currentZ }
                );
            }

            targetZ = -2;
            currentZ = -5;
            Dispatcher.Invoke(
                new UpdateSetZPos(this.SetZPos),
                new object[] { currentZ }
            );
            MoveRoomsAuto();
            Dispatcher.Invoke(
                new TriggerBuildCurrentLocation(this.BuildCurrentLocation),
                new object[] { }
            );

            moveDelta = (targetZ - currentZ) / ticks;
            for (int i = 0; i < ticks; i++)
            {
                Thread.Sleep(THREAD_SLEEP);
                currentZ += moveDelta;
                Dispatcher.Invoke(
                    new UpdateSetZPos(this.SetZPos),
                    new object[] { currentZ }
                );
            }

            acceptInput = true;
        }

        private delegate void UpdateSetZPos(double z);

        private void SetZPos(double z)
        {
            var pt = camMain.Position;
            pt.Z = z;
            camMain.Position = pt;
        }
    }
}

