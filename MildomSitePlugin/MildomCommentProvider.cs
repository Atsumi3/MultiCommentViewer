﻿using Common;
using ryu_s.BrowserCookie;
using SitePlugin;
using SitePluginCommon;
using System;
using System.Net;
using System.Threading.Tasks;
using SitePluginCommon.AutoReconnection;
using System.Collections.Generic;

namespace MildomSitePlugin
{
    interface IMyUserInfo
    {
        string UserId { get; }
        string Loginname { get; }
    }
    public class LoggedinUserInfo : IMyUserInfo
    {
        public string MyId { get; }
        public string UserId { get; }
        public string Loginname { get; }
        public Guid AccessToken { get; }
        public LoggedinUserInfo(Low.UserInfo.RootObject low)
        {
            MyId = low.MyId.ToString();
            UserId = low.UserId.ToString();
            Loginname = low.Loginname;
            AccessToken = low.AccessToken;
        }
    }
    public class AnonymousUserInfo : IMyUserInfo
    {
        public string GuestId { get; }
        public string GuestName { get; }
        string IMyUserInfo.UserId => GuestId;
        string IMyUserInfo.Loginname => GuestName;
        public AnonymousUserInfo(string guestId, string guestName)
        {
            GuestId = guestId;
            GuestName = guestName;
        }
    }
    class MildomCommentProvider : CommentProviderBase
    {
        FirstCommentDetector _first = new FirstCommentDetector();
        private readonly IDataServer _server;
        private readonly ILogger _logger;
        private readonly ICommentOptions _options;
        private readonly IMildomSiteOptions _siteOptions;
        private readonly IUserStoreManager _userStoreManager;

        public override async Task ConnectAsync(string input, IBrowserProfile browserProfile)
        {
            BeforeConnect();
            try
            {
                await ConnectInternalAsync(input, browserProfile);
            }
            catch (Exception ex)
            {
                _logger.LogException(ex, "", $"input={input}");
            }
            finally
            {
                AfterDisconnected();
            }
        }
        NewAutoReconnector _autoReconnector;
        private async Task ConnectInternalAsync(string input, IBrowserProfile browserProfile)
        {
            _isBeeingSentInitialComments = true;
            var mayBeRoomId = Tools.ExtractRoomId(input);
            if (!mayBeRoomId.HasValue)
            {
                //不正なURL
                return;
            }
            var roomId = mayBeRoomId.Value;
            var cc = GetCookieContainer(browserProfile);
            //var imageDict = await Api.GetImageDictionary(_server);
            var imageDict = await Api.GetImageDictionary(_server, roomId, cc);

            //TODO:websocketUrlをAPI経由で取得する
            //https://im.mildom.com/?room_id=10045175&type=chat&call=get_server&cluster=aws_japan
            var websocketUrl = "wss://jp-room1.mildom.com/?roomId=" + roomId;
            var p1 = new MessageProvider(new WebSocket(websocketUrl), _logger, imageDict);
            p1.MessageReceived += P1_MessageReceived;
            p1.MetadataUpdated += P1_MetadataUpdated;
            //var p2 = new MetadataProvider2(_server, _siteOptions);
            //p2.MetadataUpdated += P2_MetadataUpdated;
            //p2.Master = p1;
            try
            {
                var dummy = new DummyImpl(_server, input, browserProfile, _logger, _siteOptions, p1);//, p2);
                var connectionManager = new ConnectionManager(_logger);
                _autoReconnector = new NewAutoReconnector(connectionManager, dummy, new MessageUntara(), _logger);
                await _autoReconnector.AutoReconnectAsync();
            }
            finally
            {
                p1.MessageReceived -= P1_MessageReceived;
                p1.MetadataUpdated -= P1_MetadataUpdated;
                //p2.MetadataUpdated -= P2_MetadataUpdated;
            }
        }

        private void P2_MetadataUpdated(object sender, ILiveInfo e)
        {

        }

        private void P1_MetadataUpdated(object sender, IMetadata e)
        {

        }

        private void P1_MessageReceived(object sender, IInternalMessage e)
        {
            var message = e;
            if (message is OnBroadcast)
            {
                _isBeeingSentInitialComments = false;
            }
            var messageContext = CreateMessageContext(message);
            if (messageContext != null)
            {
                RaiseMessageReceived(messageContext);
            }
        }
        /// <summary>
        /// 初期コメントが送られてきているか。
        /// 接続直後はサーバから初期コメントが送られてくる。その後OnBroadcastメッセージが来るから、それ以降はリアルタイムのコメント。
        /// </summary>
        bool _isBeeingSentInitialComments;
        private MildomMessageContext CreateMessageContext(IInternalMessage message)
        {
            if (message is OnChatMessage chat)
            {
                var userId = chat.UserId.ToString();
                var isFirst = _first.IsFirstComment(userId);
                var user = GetUser(userId);
                var comment = new MildomComment(chat, chat.Raw);
                var metadata = new CommentMessageMetadata(comment, _options, _siteOptions, user, this, isFirst)
                {
                    IsInitialComment = _isBeeingSentInitialComments,
                    SiteContextGuid = SiteContextGuid,
                };
                var methods = new MildomMessageMethods();
                if (_siteOptions.NeedAutoSubNickname)
                {
                    var messageText = chat.MessageItems.ToText();
                    var nick = SitePluginCommon.Utils.ExtractNickname(messageText);
                    if (!string.IsNullOrEmpty(nick))
                    {
                        user.Nickname = nick;
                    }
                }
                return new MildomMessageContext(comment, metadata, methods);
            }
            //if (message is IMildomComment comment)
            //{
            //    var userId = comment.UserId;
            //    var isFirst = _first.IsFirstComment(userId);
            //    var user = GetUser(userId);
            //    //var comment = new MildomComment(message, raw);
            //    var metadata = new CommentMessageMetadata(comment, _options, _siteOptions, user, this, isFirst)
            //    {
            //        IsInitialComment = false,
            //        SiteContextGuid = SiteContextGuid,
            //    };
            //    var methods = new MildomMessageMethods();
            //    if (_siteOptions.NeedAutoSubNickname)
            //    {
            //        var messageText = message.CommentItems.ToText();
            //        var nick = SitePluginCommon.Utils.ExtractNickname(messageText);
            //        if (!string.IsNullOrEmpty(nick))
            //        {
            //            user.Nickname = nick;
            //        }
            //    }
            //    return new MildomMessageContext(comment, metadata, methods);
            //}
            //else if (message is IMildomJoinRoom join && _siteOptions.IsShowJoinMessage)
            //{
            //    var userId = join.UserId;
            //    var user = GetUser(userId);
            //    var metadata = new JoinMessageMetadata(join, _options, _siteOptions, user, this)
            //    {
            //        IsInitialComment = false,
            //        SiteContextGuid = SiteContextGuid,
            //    };
            //    var methods = new MildomMessageMethods();
            //    return new MildomMessageContext(join, metadata, methods);
            //}
            ////else if (message is IMildomItem item)
            ////{
            ////    var userId = item.UserId;
            ////    var isFirst = false;
            ////    var user = GetUser(userId);
            ////    var metadata = new  MildomMessageMetadata(item, _options, _siteOptions, user, this, isFirst)
            ////    {
            ////        IsInitialComment = false,
            ////        SiteContextGuid = SiteContextGuid,
            ////    };
            ////    var methods = new MildomMessageMethods();
            ////    return new MildomMessageContext(item, metadata, methods);
            ////}
            //else if (message is IMildomConnected connected)
            //{
            //    var metadata = new ConnectedMessageMetadata(connected, _options, _siteOptions)
            //    {
            //        IsInitialComment = false,
            //        SiteContextGuid = SiteContextGuid,
            //    };
            //    var methods = new MildomMessageMethods();
            //    return new MildomMessageContext(connected, metadata, methods);
            //}
            //else if (message is IMildomDisconnected disconnected)
            //{
            //    var metadata = new DisconnectedMessageMetadata(disconnected, _options, _siteOptions)
            //    {
            //        IsInitialComment = false,
            //        SiteContextGuid = SiteContextGuid,
            //    };
            //    var methods = new MildomMessageMethods();
            //    return new MildomMessageContext(disconnected, metadata, methods);
            //}
            else
            {
                return null;
            }
        }
        public override void Disconnect()
        {
            _autoReconnector?.Disconnect();
        }


        protected virtual CookieContainer GetCookieContainer(IBrowserProfile browserProfile)
        {
            var cc = new CookieContainer();

            try
            {
                var cookies = browserProfile.GetCookieCollection("mildom.com");
                foreach (var cookie in cookies)
                {
                    cc.Add(cookie);
                }
            }
            catch (Exception ex)
            {
                _logger.LogException(ex);
            }
            return cc;
        }
        public override async Task<ICurrentUserInfo> GetCurrentUserInfo(IBrowserProfile browserProfile)
        {
            await Task.Yield();
            var userInfo = Tools.GetUserInfoFromCookie(browserProfile);
            if (userInfo != null)
            {

                return new CurrentUserInfo
                {
                    IsLoggedIn = true,
                    UserId = userInfo.UserId.ToString(),
                    Username = userInfo.Loginname,
                };
            }
            else
            {
                return new CurrentUserInfo
                {
                    IsLoggedIn = false,
                    UserId = "",
                    Username = "",
                };
            }
        }

        public override IUser GetUser(string userId)
        {
            return _userStoreManager.GetUser(SiteType.Mildom, userId);
        }

        public override Task PostCommentAsync(string text)
        {
            throw new NotImplementedException();
        }
        public MildomCommentProvider(IDataServer server, ILogger logger, ICommentOptions options, IMildomSiteOptions siteOptions, IUserStoreManager userStoreManager)
            : base(logger, options)
        {
            _server = server;
            _logger = logger;
            _options = options;
            _siteOptions = siteOptions;
            _userStoreManager = userStoreManager;
        }
    }
    class CurrentUserInfo : ICurrentUserInfo
    {
        public string Username { get; set; }
        public string UserId { get; set; }
        public bool IsLoggedIn { get; set; }
    }
}
