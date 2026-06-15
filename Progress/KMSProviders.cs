using MongoObject.PropertyEncryption.Attributes;

namespace Progress.Kms
{
    [KMSProviders]
    public class KMSProviders
    {
        [KMSLocal("crypt-master.key.bin")]
        public string Local{ get; set; } = string.Empty;
        //[KMSAws(AccessKeyPath = "Aws:AccessKeyPath", SecretKeyPath = "Aws:SecretKey")]
        public string Aws { get; set; } = string.Empty;
        //[KMSAzure]
        public string Azure {  get; set; } = string.Empty;
    }
}
