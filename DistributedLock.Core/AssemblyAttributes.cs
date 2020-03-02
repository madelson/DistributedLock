using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

[assembly: InternalsVisibleTo("DistributedLock.Tests")]

// Note: we allow for internals sharing in release only. This allows us to have certain
// internal APIs which are public in DEBUG and internal in RELEASE. That way, we can't
// build in DEBUG if we rely on internal APIs that are not meant to be public
#if !DEBUG
[assembly: InternalsVisibleTo("DistributedLock.EventWaitHandles")]
[assembly: InternalsVisibleTo("DistributedLock.Postgres")]
#endif
