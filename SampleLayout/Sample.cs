using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

using TomShane.Neoforce.Controls;
using nfConsole = TomShane.Neoforce.Controls;

using LayoutContract;

using Dashboard.Library;

namespace SampleLayout
{
    public class Sample : ILayout
    {
        private enum NetMessageType
        {
            // in
            BACK_SHOOTER_DATA_SPEED = 0x00,
            FRONT_SHOOTER_DATA_SPEED = 0x10,
            BACK_SHOOTER_DATA_CURRENT = 0x01,
            FRONT_SHOOTER_DATA_CURRENT = 0x11,
            PTO_DATA = 0x20,
            WINCH_DATA = 0x30,
            GENERAL_DATA = 0x40,
            AUTO_AIM_DATA = 0x50,
            DISC_LOCATOR_DATA = 0x60,
            ROBOT_TELEMETRY = 0x70,
            ROBOT_TELEMETRY_INIT = 0x80,
            VIDEO = 0x90,

            // out
            DASHBOARD_DATA = 0x20,
            ROBOTDATA_DATA = 0x21,
        }

        public void SetupLayout(Manager manager, ContentLibrary contentLibrary)
        {
            TabControl tabControl = (TabControl)manager.GetControl("DB_2.0_TAB_ROOT");

            TabPage climber = tabControl.AddPageBeforeEnd();
            climber.Init();
            climber.Text = "Climber";

            TabPage shooter = tabControl.AddPageBeforeEnd();
            shooter.Init();
            shooter.Text = "Shooter";

            TabPage inGame = tabControl.AddPageBeforeEnd();
            inGame.Init();
            inGame.Text = "Main Game";

            //ImageBox shooterOuter = new ImageBox(manager);
            //shooterOuter.Width = shooter.Width;
            //shooterOuter.Parent = shooter;
            //shooterOuter.Image = new Texture2D(GraphicsDevice, 100, 100);

            Label innerShooterLabel = new Label(manager);
            innerShooterLabel.Init();
            innerShooterLabel.Text = "Inner Shooter";
            innerShooterLabel.Width = shooter.ClientWidth;
            innerShooterLabel.Top = 2;
            innerShooterLabel.Left = 2;
            innerShooterLabel.Parent = shooter;

            Graph innerShooter = new Graph(manager, "shooter_inner", contentLibrary);
            innerShooter.SubscribeToPacket((byte)NetMessageType.BACK_SHOOTER_DATA_SPEED);
            innerShooter.Init();
            innerShooter.Left = 2;
            innerShooter.Width = shooter.ClientWidth - 4;
            innerShooter.Top = 2 + innerShooterLabel.AbsoluteRect.Height;
            innerShooter.Height = shooter.ClientHeight / 2 - innerShooter.Top;
            innerShooter.Parent = shooter;

            Label outerShooterLabel = new Label(manager);
            outerShooterLabel.Init();
            outerShooterLabel.Text = "Outer Shooter";
            outerShooterLabel.Width = shooter.ClientWidth;
            outerShooterLabel.Top = shooter.ClientHeight / 2 + 2;
            outerShooterLabel.Left = 2;
            outerShooterLabel.Parent = shooter;

            Graph outerShooter = new Graph(manager, "shooter_outer", contentLibrary);
            outerShooter.Init();
            outerShooter.SubscribeToPacket((byte)NetMessageType.FRONT_SHOOTER_DATA_SPEED);
            outerShooter.Left = 2;
            outerShooter.Width = shooter.ClientWidth - 4;
            outerShooter.Top = shooter.ClientHeight / 2 + 2 + outerShooterLabel.AbsoluteRect.Height;
            outerShooter.Height = shooter.ClientHeight - outerShooter.Top;
            outerShooter.Parent = shooter;

            TabPage telemetry = tabControl.AddPageBeforeEnd();
            telemetry.Init();
            telemetry.Text = "Telemetry";
        }
    }
}
