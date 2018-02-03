﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System.Windows.Input;
using SitePlugin;
using System.Threading;
using System.Collections.ObjectModel;
using Plugin;
using ryu_s.BrowserCookie;
using System.Diagnostics;
using System.Windows.Threading;
using System.Net;
using System.Windows.Media;

//TODO:過去コメントの取得


namespace MultiCommentViewer
{
    public class MainViewModel: ViewModelBase
    {
        #region Commands
        public ICommand MainViewContentRenderedCommand { get; }
        public ICommand MainViewClosingCommand { get; }
        public ICommand ShowOptionsWindowCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand ShowWebSiteCommand { get; }
        public ICommand ShowDevelopersTwitterCommand { get; }
        public ICommand CheckUpdateCommand { get; }
        public ICommand ShowUserInfoCommand { get; }
        public ICommand RemoveSelectedConnectionCommand { get; }
        public ICommand AddNewConnectionCommand { get; }
        #endregion //Commands

        #region Fields
        IOptions _options;
        IEnumerable<ISiteContext> _siteContexts;
        IEnumerable<SiteViewModel> _siteVms;
        IEnumerable<BrowserViewModel> _browserVms;

        IEnumerable<IPlugin> _plugins;
        ISitePluginManager _siteManager;
        private readonly Dispatcher _dispatcher;
        #endregion //Fields


        #region Methods
        private void ShowOptionsWindow()
        {
            var list = new List<IOptionsTabPage>();
            var mainOptionsPanel = new MainOptionsPanel();
            mainOptionsPanel.SetViewModel(new MainOptionsViewModel(_options));
            list.Add(new MainTabPage("一般", mainOptionsPanel));
            foreach(var site in _siteContexts)
            {
                try
                {
                    list.Add(site.TabPanel);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
            MessengerInstance.Send(new ShowOptionsViewMessage(list));
        }
        private void ContentRendered()
        {
            try
            {


                //ISitePluginManager _siteManager = DiContainer.Instance.GetNewInstance<ISitePluginManager>();
                //_siteManager.LoadSitePlugins(_options);
                ISitePluginLoader sitePluginLoader = DiContainer.Instance.GetNewInstance<ISitePluginLoader>();
                _siteContexts = sitePluginLoader.LoadSitePlugins(_options);
                foreach (var site in _siteContexts)
                {
                    site.LoadOptions(_options.SettingsDirPath);
                }
                _siteVms = _siteContexts.Select(c => new SiteViewModel(c));

                IBrowserLoader browserLoader = DiContainer.Instance.GetNewInstance<IBrowserLoader>();
                _browserVms = browserLoader.LoadBrowsers().Select(b => new BrowserViewModel(b));
                //もしブラウザが無かったらclass EmptyBrowserProfileを使う。
                if (_browserVms.Count() == 0)
                {
                    _browserVms = new List<BrowserViewModel>
                    {
                        new BrowserViewModel( new EmptyBrowserProfile()),
                    };
                }

                _pluginManager.LoadPlugins(new PluginHost(this, _options));

                _pluginManager.OnLoaded();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        private void Closing()
        {
            try
            {
                foreach (var site in _siteContexts)
                {
                    site.SaveOptions(_options.SettingsDirPath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        #endregion //Methods


        
        private void RemoveSelectedConnection()
        {
            var toRemove = Connections.Where(conn => conn.IsSelected).ToList();
            foreach (var conn in toRemove)
            {
                Connections.Remove(conn);
                var meta = _metaDict[conn];
                _metaDict.Remove(conn);
                MetaCollection.Remove(meta);
            }
            //TODO:この接続に関連するコメントも全て消したい
        }
        
        private void AddNewConnection()
        {
            try
            {
                var connectionName = new ConnectionName();//TODO:一意の名前を設定
                var connection = new ConnectionViewModel(connectionName, _siteVms, _browserVms);
                connection.CommentsReceived += Connection_CommentsReceived;
                connection.MetadataReceived += Connection_MetadataReceived;
                var metaVm = new MetadataViewModel(connectionName);
                _metaDict.Add(connection, metaVm);
                MetaCollection.Add(metaVm);
                Connections.Add(connection);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debugger.Break();
            }
        }


        #region Properties
        public ObservableCollection<ICommentViewModel> Comments { get; } = new ObservableCollection<ICommentViewModel>();
        public ObservableCollection<ConnectionViewModel> Connections { get; } = new ObservableCollection<ConnectionViewModel>();
        public string Title
        {
            get
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                var ver = asm.GetName().Version;
                var title = asm.GetName().Name;
                var s = $"{title} v{ver.Major}.{ver.Minor}.{ver.Build}";
#if DEBUG
                s += " (DEBUG)";
#endif
                return s;
            }
        }
        public bool Topmost { get { return false; } }
        public double MainViewHeight
        {
            get { return _options.MainViewHeight; }
            set { _options.MainViewHeight = value; }
        }
        public double MainViewWidth
        {
            get { return _options.MainViewWidth; }
            set { _options.MainViewWidth = value; }
        }
        public double MainViewLeft
        {
            get { return _options.MainViewLeft; }
            set { _options.MainViewLeft = value; }
        }
        public double MainViewTop
        {
            get { return _options.MainViewTop; }
            set { _options.MainViewTop = value; }
        }
        public Brush HorizontalGridLineBrush
        {
            get { return new SolidColorBrush(_options.HorizontalGridLineColor); }
        }
        public Brush VerticalGridLineBrush
        {
            get { return new SolidColorBrush(_options.VerticalGridLineColor); }
        }
        public double ConnectionNameWidth
        {
            get { return _options.ConnectionNameWidth; }
            set { _options.ConnectionNameWidth = value; }
        }
        public bool IsShowConnectionName
        {
            get { return _options.IsShowConnectionName; }
            set { _options.IsShowConnectionName = value; }
        }
        public int ConnectionNameDisplayIndex
        {
            get { return _options.ConnectionNameDisplayIndex; }
            set { _options.ConnectionNameDisplayIndex = value; }
        }
        public double ThumbnailWidth
        {
            get { return _options.ThumbnailWidth; }
            set { _options.ThumbnailWidth = value; }
        }
        public bool IsShowThumbnail
        {
            get { return _options.IsShowThumbnail; }
            set { _options.IsShowThumbnail = value; }
        }
        public int ThumbnailDisplayIndex
        {
            get { return _options.ThumbnailDisplayIndex; }
            set { _options.ThumbnailDisplayIndex = value; }
        }
        public double CommentIdWidth
        {
            get { return _options.CommentIdWidth; }
            set { _options.CommentIdWidth = value; }
        }
        public bool IsShowCommentId
        {
            get { return _options.IsShowCommentId; }
            set { _options.IsShowCommentId = value; }
        }
        public int CommentIdDisplayIndex
        {
            get { return _options.CommentIdDisplayIndex; }
            set { _options.CommentIdDisplayIndex = value; }
        }
        public double UsernameWidth
        {
            get { return _options.UsernameWidth; }
            set { _options.UsernameWidth = value; }
        }
        public bool IsShowUsername
        {
            get { return _options.IsShowUsername; }
            set { _options.IsShowUsername = value; }
        }
        public int UsernameDisplayIndex
        {
            get { return _options.UsernameDisplayIndex; }
            set { _options.UsernameDisplayIndex = value; }
        }

        public double MessageWidth
        {
            get { return _options.MessageWidth; }
            set { _options.MessageWidth = value; }
        }
        public bool IsShowMessage
        {
            get { return _options.IsShowMessage; }
            set { _options.IsShowMessage = value; }
        }
        public int MessageDisplayIndex
        {
            get { return _options.MessageDisplayIndex; }
            set { _options.MessageDisplayIndex = value; }
        }

        public double InfoWidth
        {
            get { return _options.InfoWidth; }
            set { _options.InfoWidth = value; }
        }
        public bool IsShowInfo
        {
            get { return _options.IsShowInfo; }
            set { _options.IsShowInfo = value; }
        }
        public int InfoDisplayIndex
        {
            get { return _options.InfoDisplayIndex; }
            set { _options.InfoDisplayIndex = value; }
        }
        public Color SelectedRowBackColor
        {
            get { return _options.SelectedRowBackColor; }
            set { _options.SelectedRowBackColor = value; }
        }
        public Color SelectedRowForeColor
        {
            get { return _options.SelectedRowForeColor; }
            set { _options.SelectedRowForeColor = value; }
        }
        #endregion

        Dictionary<string, UserViewModel> _userDict = new Dictionary<string, UserViewModel>();
        private async void Connection_CommentsReceived(object sender, List<ICommentViewModel> e)
        {
            //TODO:Comments.AddRange()が欲しい
            await _dispatcher.BeginInvoke((Action)(() =>
            {
                foreach (var comment in e)
                {
                    if(!_userDict.TryGetValue(comment.UserId, out UserViewModel uvm))
                    {
                        var user = _userStore.Get(comment.UserId);
                        uvm = new UserViewModel(user, _options);
                        _userDict.Add(comment.UserId, uvm);
                    }
                    comment.User = uvm.User;
                    Comments.Add(comment);
                    uvm.Comments.Add(comment);                    
                }
            }), DispatcherPriority.Normal);
            try
            {
                _pluginManager.SetComments(e);
            }catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        Dictionary<ConnectionViewModel, MetadataViewModel> _metaDict = new Dictionary<ConnectionViewModel, MetadataViewModel>();
        public ObservableCollection<MetadataViewModel> MetaCollection { get; } = new ObservableCollection<MetadataViewModel>();

        public ObservableCollection<PluginMenuItemViewModel> PluginMenuItemCollection { get; } = new ObservableCollection<PluginMenuItemViewModel>();
        private void Connection_MetadataReceived(object sender, IMetadata e)
        {
            if (sender is ConnectionViewModel connection)
            {
                var metaVm = _metaDict[connection];
                if (e.Title != null)
                    metaVm.Title = e.Title;
                if (e.Active != null)
                    metaVm.Active = e.Active;
            }
        }
        private readonly IUserStore _userStore;
        
        public ICommand ClearAllCommentsCommand { get; }
        private void ClearAllComments()
        {
            Comments.Clear();
            //個別ユーザのコメントはどうしようか
        }
        private readonly IPluginManager _pluginManager;
        public MainViewModel()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            //読み込み
            IOptionsLoader optionsLoader = DiContainer.Instance.GetNewInstance<IOptionsLoader>();
            _options = optionsLoader.LoadOptions();
            _userStore = DiContainer.Instance.GetNewInstance<IUserStore>();
            _pluginManager = new PluginManager(_options);
            _pluginManager.PluginAdded += PluginManager_PluginAdded;

            MainViewContentRenderedCommand = new RelayCommand(ContentRendered);
            MainViewClosingCommand = new RelayCommand(Closing);
            ShowOptionsWindowCommand = new RelayCommand(ShowOptionsWindow);
            ExitCommand = new RelayCommand(Exit);
            ShowWebSiteCommand = new RelayCommand(ShowWebSite);
            ShowDevelopersTwitterCommand = new RelayCommand(ShowDevelopersTwitter);
            CheckUpdateCommand = new RelayCommand(CheckUpdate);
            AddNewConnectionCommand = new RelayCommand(AddNewConnection);
            RemoveSelectedConnectionCommand = new RelayCommand(RemoveSelectedConnection);
            ClearAllCommentsCommand = new RelayCommand(ClearAllComments);
            ShowUserInfoCommand = new RelayCommand(ShowUserInfo);
        }
        private readonly Dictionary<IPlugin, PluginMenuItemViewModel> _pluginMenuItemDict = new Dictionary<IPlugin, PluginMenuItemViewModel>();
        private void PluginManager_PluginAdded(object sender, IPlugin e)
        {
            var vm = new PluginMenuItemViewModel(e);
            _pluginMenuItemDict.Add(e, vm);
            PluginMenuItemCollection.Add(vm);
        }

        private ICommentViewModel _selectedComment;
        public ICommentViewModel SelectedComment
        {
            get { return _selectedComment; }
            set
            {
                _selectedComment = value;
            }
        }
        private void ShowUserInfo()
        {
            var current = SelectedComment;
            try
            {
                Debug.Assert(current != null);
                Debug.Assert(current is ICommentViewModel);
                var uvm = _userDict[current.UserId];
                MessengerInstance.Send(new ShowUserViewMessage(uvm));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        private void CheckUpdate()
        {

        }
        private void ShowDevelopersTwitter()
        {

        }
        private void ShowWebSite()
        {

        }
        private void Exit()
        {

        }
    }
    public class PluginHost : IPluginHost
    {
        public string SettingsDirPath => _options.SettingsDirPath;

        public double MainViewLeft => _options.MainViewLeft;

        public double MainViewTop => _options.MainViewTop;
        private readonly MainViewModel _vm;
        private readonly IOptions _options;
        public PluginHost(MainViewModel vm, IOptions options)
        {
            _vm = vm;
            _options = options;
        }
    }
    public class CommentData : Plugin.ICommentData
    {
        public string Id { get; set; }

        public string UserId { get; set; }

        public string Nickname { get; set; }

        public string Comment { get; set; }
        public bool IsNgUser { get; set; }
    }
    public class EmptyBrowserProfile : IBrowserProfile
    {
        public string Path => "";

        public string ProfileName => "無し";

        public BrowserType Type { get { return BrowserType.Unknown; } }

        public Cookie GetCookie(string domain, string name)
        {
            return null;
        }

        public CookieCollection GetCookieCollection(string domain)
        {
            return new CookieCollection();
        }
    }
    public interface IBrowserLoader
    {
        IEnumerable<IBrowserProfile> LoadBrowsers();
    }
    public class BrowserLoader : IBrowserLoader
    {
        public IEnumerable<IBrowserProfile> LoadBrowsers()
        {
            var list = new List<IBrowserProfile>();
            list.AddRange(new ChromeManager().GetProfiles());
            return list;
        }
    }
    public interface IUserStore
    {
        IUser Get(string userid);
    }
    public class UserTest : IUser
    {
        public string UserId { get { return _userid; } }
        public string ForeColorArgb { get; set; }
        public string BackColorArgb { get; set; }

        private string _nickname;
        public string Nickname
        {
            get { return _nickname; }
            set
            {
                if (_nickname == value)
                    return;
                _nickname = value;
                RaisePropertyChanged();
            }
        }
        private readonly string _userid;
        public UserTest(string userId)
        {
            _userid = userId;
        }
        #region INotifyPropertyChanged
        [NonSerialized]
        private System.ComponentModel.PropertyChangedEventHandler _propertyChanged;
        /// <summary>
        /// 
        /// </summary>
        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged
        {
            add { _propertyChanged += value; }
            remove { _propertyChanged -= value; }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        protected void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = "")
        {
            _propertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
    public class UserStoreTest : IUserStore
    {
        Dictionary<string, IUser> _dict = new Dictionary<string, IUser>();
        public IUser Get(string userid)
        {
            if (!_dict.TryGetValue(userid, out IUser user))
            {
                user = new UserTest(userid);
                _dict.Add(userid, user);
            }
            return user;
        }
    }
    public class PluginMenuItemViewModel
    {
        public string Name { get; set; }
        public ObservableCollection<PluginMenuItemViewModel> Children { get; } = new ObservableCollection<PluginMenuItemViewModel>();
        public ICommand Command { get; }
        public PluginMenuItemViewModel(IPlugin plugin)// PluginContext plugin, string name, ICommand command)
        {
            Name = plugin.Name;

            //TODO:exeファイルを直接実行するとTest()が呼び出されるが、Visual Studioのデバッグモードだと呼び出されない。なぜ？？
            Command = new Ttk.RelayCommand(()=>Test(plugin));
        }
        private void Test(IPlugin plugin)
        {
            try
            {
                //var x = host.MainViewLeft;
                //var y = host.MainViewTop;
                //var width = host.MainViewWidth;
                //var height = host.MainViewHeight;
                plugin.ShowSettingView();// x, y, width, height);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
    }
}




namespace Ttk
{
    using GalaSoft.MvvmLight.Helpers;
    using System;
    using System.Runtime.CompilerServices;
    using System.Windows.Input;
    /// <summary>
    /// A command whose sole purpose is to relay its functionality to other
    /// objects by invoking delegates. The default return value for the CanExecute
    /// method is 'true'.  This class does not allow you to accept command parameters in the
    /// Execute and CanExecute callback methods.
    /// </summary>
    /// <remarks>If you are using this class in WPF4.5 or above, you need to use the 
    /// GalaSoft.MvvmLight.CommandWpf namespace (instead of GalaSoft.MvvmLight.Command).
    /// This will enable (or restore) the CommandManager class which handles
    /// automatic enabling/disabling of controls based on the CanExecute delegate.</remarks>
    public class RelayCommand : ICommand
    {
        private readonly WeakAction _execute;

        private readonly WeakFunc<bool> _canExecute;

        /// <summary>
        /// Occurs when changes occur that affect whether the command should execute.
        /// </summary>
        [method: CompilerGenerated]
        [CompilerGenerated]
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Initializes a new instance of the RelayCommand class that 
        /// can always execute.
        /// </summary>
        /// <param name="execute">The execution logic. IMPORTANT: Note that closures are not supported at the moment
        /// due to the use of WeakActions (see http://stackoverflow.com/questions/25730530/). </param>
        /// <exception cref="T:System.ArgumentNullException">If the execute argument is null.</exception>
        public RelayCommand(Action execute) : this(execute, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the RelayCommand class.
        /// </summary>
        /// <param name="execute">The execution logic. IMPORTANT: Note that closures are not supported at the moment
        /// due to the use of WeakActions (see http://stackoverflow.com/questions/25730530/). </param>
        /// <param name="canExecute">The execution status logic.</param>
        /// <exception cref="T:System.ArgumentNullException">If the execute argument is null. IMPORTANT: Note that closures are not supported at the moment
        /// due to the use of WeakActions (see http://stackoverflow.com/questions/25730530/). </exception>
        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }
            this._execute = new WeakAction(execute);
            if (canExecute != null)
            {
                this._canExecute = new WeakFunc<bool>(canExecute);
            }
        }

        /// <summary>
        /// Raises the <see cref="E:GalaSoft.MvvmLight.Command.RelayCommand.CanExecuteChanged" /> event.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            EventHandler canExecuteChanged = this.CanExecuteChanged;
            if (canExecuteChanged != null)
            {
                canExecuteChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">This parameter will always be ignored.</param>
        /// <returns>true if this command can be executed; otherwise, false.</returns>
        public bool CanExecute(object parameter)
        {
            return this._canExecute == null || ((this._canExecute.IsStatic || this._canExecute.IsAlive) && this._canExecute.Execute());
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked. 
        /// </summary>
        /// <param name="parameter">This parameter will always be ignored.</param>
        public virtual void Execute(object parameter)
        {
            if (this.CanExecute(parameter) && this._execute != null && (this._execute.IsStatic || this._execute.IsAlive))
            {
                this._execute.Execute();
            }
        }
    }
}