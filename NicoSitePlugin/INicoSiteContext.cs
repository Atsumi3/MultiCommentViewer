﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SitePlugin;
namespace NicoSitePlugin.Test
{
    public interface INicoSiteContext : ISiteContext
    {
        INicoCommentProvider CreateNicoCommentProvider();
        INicoSiteOptions GetNicoSiteOptions();
    }
}