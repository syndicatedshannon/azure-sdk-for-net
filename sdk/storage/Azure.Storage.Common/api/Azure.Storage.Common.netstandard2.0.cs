namespace Azure.Storage
{
    public sealed partial class StorageSharedKeyCredential
    {
        public StorageSharedKeyCredential(string accountName, string accountKey) { }
        public string AccountName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public void SetAccountKey(string accountKey) { }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public partial struct StorageTransferOptions : System.IEquatable<Azure.Storage.StorageTransferOptions>
    {
        public int? MaximumConcurrency { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public int? MaximumTransferLength { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public bool Equals(Azure.Storage.StorageTransferOptions obj) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals(object obj) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static bool operator ==(Azure.Storage.StorageTransferOptions left, Azure.Storage.StorageTransferOptions right) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public static bool operator !=(Azure.Storage.StorageTransferOptions left, Azure.Storage.StorageTransferOptions right) { throw null; }
    }
}
namespace Azure.Storage.Sas
{
    public partial class AccountSasBuilder
    {
        public AccountSasBuilder() { }
        public System.DateTimeOffset ExpiresOn { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Azure.Storage.Sas.SasIPRange IPRange { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Permissions { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public Azure.Storage.Sas.SasProtocol Protocol { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Azure.Storage.Sas.AccountSasResourceTypes ResourceTypes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public Azure.Storage.Sas.AccountSasServices Services { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public System.DateTimeOffset StartsOn { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        public string Version { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } [System.Runtime.CompilerServices.CompilerGeneratedAttribute] set { } }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals(object obj) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() { throw null; }
        public void SetPermissions(Azure.Storage.Sas.AccountSasPermissions permissions) { }
        public void SetPermissions(string rawPermissions) { }
        public Azure.Storage.Sas.SasQueryParameters ToSasQueryParameters(Azure.Storage.StorageSharedKeyCredential sharedKeyCredential) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public override string ToString() { throw null; }
    }
    [System.FlagsAttribute]
    public enum AccountSasPermissions
    {
        All = -1,
        Read = 1,
        Write = 2,
        Delete = 4,
        List = 8,
        Add = 16,
        Create = 32,
        Update = 64,
        Process = 128,
    }
    [System.FlagsAttribute]
    public enum AccountSasResourceTypes
    {
        All = -1,
        Service = 1,
        Container = 2,
        Object = 4,
    }
    [System.FlagsAttribute]
    public enum AccountSasServices
    {
        All = -1,
        Blobs = 1,
        Queues = 2,
        Files = 4,
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct SasIPRange : System.IEquatable<Azure.Storage.Sas.SasIPRange>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public SasIPRange(System.Net.IPAddress start, System.Net.IPAddress end = null) { throw null; }
        public System.Net.IPAddress End { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public System.Net.IPAddress Start { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool Equals(Azure.Storage.Sas.SasIPRange other) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals(object obj) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() { throw null; }
        public static bool operator ==(Azure.Storage.Sas.SasIPRange left, Azure.Storage.Sas.SasIPRange right) { throw null; }
        public static bool operator !=(Azure.Storage.Sas.SasIPRange left, Azure.Storage.Sas.SasIPRange right) { throw null; }
        public static Azure.Storage.Sas.SasIPRange Parse(string s) { throw null; }
        public override string ToString() { throw null; }
    }
    public enum SasProtocol
    {
        None = 0,
        HttpsAndHttp = 1,
        Https = 2,
    }
    public partial class SasQueryParameters
    {
        internal SasQueryParameters() { }
        public const string DefaultSasVersion = "2019-02-02";
        public string CacheControl { get { throw null; } }
        public string ContentDisposition { get { throw null; } }
        public string ContentEncoding { get { throw null; } }
        public string ContentLanguage { get { throw null; } }
        public string ContentType { get { throw null; } }
        public static Azure.Storage.Sas.SasQueryParameters Empty { get { throw null; } }
        public System.DateTimeOffset ExpiresOn { get { throw null; } }
        public string Identifier { get { throw null; } }
        public Azure.Storage.Sas.SasIPRange IPRange { get { throw null; } }
        public string Permissions { get { throw null; } }
        public Azure.Storage.Sas.SasProtocol Protocol { get { throw null; } }
        public string Resource { get { throw null; } }
        public Azure.Storage.Sas.AccountSasResourceTypes? ResourceTypes { get { throw null; } }
        public Azure.Storage.Sas.AccountSasServices? Services { get { throw null; } }
        public string Signature { get { throw null; } }
        public System.DateTimeOffset StartsOn { get { throw null; } }
        public string Version { get { throw null; } }
        public override string ToString() { throw null; }
    }
}
