using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.WatchCircle.Cleanup;

/// <summary>A provider- and file-verified external entry.</summary>
internal sealed class ArrMatch
{
    public int Id { get; set; }

    public string Path { get; set; } = string.Empty;
}
