using System;

namespace GraniteServer.Common;

public abstract class VintageStoryServerDataProxy
{
    public VintageStoryServerDataProxy() { }
}

public class LocalVintageStoryServerDataProxy : VintageStoryServerDataProxy
{
    public LocalVintageStoryServerDataProxy()
        : base() { }
}

public class RemoteVintageStoryServerDataProxy : VintageStoryServerDataProxy
{
    public RemoteVintageStoryServerDataProxy()
        : base() { }
}
