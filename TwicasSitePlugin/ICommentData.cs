﻿using System;
using System.Collections.Generic;
using SitePlugin;

namespace TwicasSitePlugin
{
    interface ICommentData
    {
        DateTime Date { get; }
        long Id { get; }
        IEnumerable<IMessagePart> Message { get; }
        string Name { get; }
        string UserId { get; }
        string ThumbnailUrl { get; }
        int ThumbnailWidth { get; }
        int ThumbnailHeight { get; }
    }
}
