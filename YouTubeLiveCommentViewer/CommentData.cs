﻿namespace YouTubeLiveCommentViewer
{
    public class CommentData : Plugin.ICommentData
    {
        public string Id { get; set; }

        public string UserId { get; set; }

        public string Nickname { get; set; }

        public string Comment { get; set; }
        public bool IsNgUser { get; set; }
    }
}