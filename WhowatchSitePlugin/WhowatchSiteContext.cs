﻿using Common;
using SitePlugin;
using SitePluginCommon;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WhowatchSitePlugin
{
    public class WhowatchSiteContext : SiteContextBase
    {
        private IWhowatchSiteOptions _siteOptions;
        private readonly ICommentOptions _options;
        private readonly IDataServer _server;
        private readonly ILogger _logger;

        public override Guid Guid => new Guid("EA695072-BABB-4FC9-AB9F-2F87D829AE7D");

        public override string DisplayName => "ふわっち";
        protected override SiteType SiteType => SiteType.Whowatch;
        public override IOptionsTabPage TabPanel
        {
            get
            {
                var panel = new TabPagePanel();
                panel.SetViewModel(new WhowatchSiteOptionsViewModel(_siteOptions));
                return new WhowatchOptionsTabPage(DisplayName, panel);
            }
        }

        public override ICommentProvider CreateCommentProvider()
        {
            return new WhowatchCommentProvider(_server, _options, _siteOptions, _userStoreManager, _logger)
            {
                SiteContextGuid = Guid,
            };
        }

        public override System.Windows.Controls.UserControl GetCommentPostPanel(ICommentProvider commentProvider)
        {
            var cp = commentProvider as WhowatchCommentProvider;
            Debug.Assert(cp != null);
            if (cp == null)
                return null;

            var vm = new CommentPostPanelViewModel(cp, _logger);
            var panel = new CommentPostPanel
            {
                //IsEnabled = false,
                DataContext = vm
            };
            return panel;
        }

        public override bool IsValidInput(string input)
        {
            return Tools.IsValidUrl(input);
        }
        protected virtual IWhowatchSiteOptions CreateWhowatchSiteOptions()
        {
            return new WhowatchSiteOptions();
        }
        public override void LoadOptions(string path, IIo io)
        {
            _siteOptions = CreateWhowatchSiteOptions();
            try
            {
                var s = io.ReadFile(path);

                _siteOptions.Deserialize(s);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _logger.LogException(ex, "", $"path={path}");
            }
        }

        public override void SaveOptions(string path, IIo io)
        {
            try
            {
                var s = _siteOptions.Serialize();
                io.WriteFile(path, s);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                _logger.LogException(ex, "", $"path={path}");
            }
        }
        protected virtual IDataServer CreateServer()
        {
            return new DataServer();
        }
        public WhowatchSiteContext(ICommentOptions options, ILogger logger, IUserStoreManager userStoreManager)
            : base(options, userStoreManager, logger)
        {
            _options = options;
            _server = CreateServer();
            _logger = logger;
        }
    }
}
