using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using TomShane.Neoforce.Controls;

using Dashboard.Library;
using Dashboard.Layouts;

namespace Dashboard
{
    public class Dashboard : Application
    {
        private ContentLibrary _contentLibrary;
        private NetConsoleProvider _ncp;

        private Console _console;
        private TabControl _tabControl;
        private MainMenu _menuStrip;

        private bool _clearNextUpdate;

        public Dashboard()
            : base("Default", false)
        {
            Graphics.PreferredBackBufferWidth = 800;
            Graphics.PreferredBackBufferHeight = 600;

            Content.RootDirectory = "Content";

            // Setting up the shared skins directory
            //Manager.SkinDirectory = @"..\..\Skins\";

            ClearBackground = true;
            BackgroundColor = new Color(50, 50, 50);
            ExitConfirmation = false;

            Manager.AutoUnfocus = false;

            _contentLibrary = new ContentLibrary();
        }

        protected override void Initialize()
        {
            base.Initialize();

            ResetLayout();
        }

        void edit_resetLayoutButton_Click(object sender, EventArgs e)
        {
            ResetLayout();
        }

        private void ResetLayout()
        {
            Manager.RemoveAll();

            NetworkManager.Clear();

            _menuStrip = new MainMenu(Manager);
            _menuStrip.Init();
            _menuStrip.Width = GraphicsDevice.Viewport.Width;

            MenuItem fileButton = new MenuItem("File");
            MenuItem file_loadlayoutButton = new MenuItem("Load Layout...");
            file_loadlayoutButton.Click += new EventHandler(file_loadlayoutButton_Click);
            fileButton.Items.Add(file_loadlayoutButton);
            MenuItem editButton = new MenuItem("Edit");
            MenuItem edit_resetLayoutButton = new MenuItem("Reset Layout...");
            edit_resetLayoutButton.Click += new EventHandler(edit_resetLayoutButton_Click);
            editButton.Items.Add(edit_resetLayoutButton);
            _menuStrip.Items.Add(fileButton);
            _menuStrip.Items.Add(editButton);

            Manager.Add(_menuStrip);

            _tabControl = new TabControl(Manager);
            _tabControl.Init();

            _tabControl.Top = _menuStrip.Height;
            _tabControl.Name = "DB_2.0_TAB_ROOT";
            _tabControl.Width = GraphicsDevice.Viewport.Width;
            _tabControl.Height = GraphicsDevice.Viewport.Height - _menuStrip.Height;
            _tabControl.Visible = true;

            TabPage console = _tabControl.AddPage();
            console.Init();
            console.Text = "Console";

            if (_console == null)
            {
                _console = new Console(Manager);
                _console.Init();
                _console.AddToConsoleOnSend = false;
                _console.Width = console.ClientWidth;
                _console.Height = console.ClientHeight;
                _console.Channels.Add(new ConsoleChannel(Constants.CONSOLE_ROBOT, "Robot", Color.Yellow));
                _console.Channels.Add(new ConsoleChannel(Constants.CONSOLE_LOCAL, "Local", Color.White));
                _console.Channels.Add(new ConsoleChannel(Constants.CONSOLE_ERROR, "Error", Color.Red));
                _console.SelectedChannel = 0;
                _console.ChannelsVisible = true;

                _ncp = new NetConsoleProvider(_console, "10.8.46.2");
                _ncp.Start();

                NetworkManager.Start("127.0.0.1", 846);
            }

            _console.Parent = console;

            Manager.Add(_tabControl);
        }

        void file_loadlayoutButton_Click(object sender, EventArgs e)
        {
            FileDialog fileDialog = new FileDialog(Manager);
            fileDialog.Init();
            fileDialog.Text = "Test";
            fileDialog.Left = 10;
            fileDialog.Top = 10;
            fileDialog.StayOnTop = true;
            fileDialog.Closing += new WindowClosingEventHandler(fileDialog_Closing);
            Manager.Add(fileDialog);
        }

        void fileDialog_Closing(object sender, WindowClosingEventArgs e)
        {
            FileDialog dialog = (FileDialog)sender;

            ModalResult result = dialog.ModalResult;

            if (result == ModalResult.Ok)
            {
                string file = dialog.FileName;

                ResetLayout();

                ILayoutLoader loader = new CompiledLayoutLoader(file);
                loader.LoadLayout(Manager, _contentLibrary);
            }
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            Content.RootDirectory = "Content";

            _contentLibrary.Load(Content, GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            NetworkManager.UpdateNetwork();

            _ncp.Update();

            base.Update(gameTime);
        }
    }
}