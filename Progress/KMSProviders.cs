using MongoObject.PropertyEncryption.Attributes;

namespace Progress
{
    [KMSProviders]
    public class KMSProviders
    {
        [KMSLocal("crypt-master.key.bin")]
        public string Local { get; set; } = string.Empty;
    }
}
