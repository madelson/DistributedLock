using Azure;
using Medallion.Threading.Internal;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Medallion.Threading.Azure;

internal static class AzureErrors
{
    public const string BlobNotFound = nameof(BlobNotFound),
        LeaseAlreadyPresent = nameof(LeaseAlreadyPresent),
        LeaseIdMissing = nameof(LeaseIdMissing);
}
