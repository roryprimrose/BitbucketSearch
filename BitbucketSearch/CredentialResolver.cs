namespace BitbucketSearch
{
    using Meziantou.Framework.Win32;

    public static class CredentialResolver
    {
        public static Credential GetCredentials(Options options)
        {
            var credentials = CredentialManager.ReadCredential(options.ServerUri);

            if (credentials != null)
            {
                return credentials;
            }

            var result = CredentialManager.PromptForCredentials(captionText: "Enter Bitbucket credentials",
                messageText: options.ServerUri,
                saveCredential: CredentialSaveOption.Selected);

            if (result == null)
            {
                return null;
            }

            credentials = new Credential(CredentialType.Generic,
                options.ServerUri,
                result.UserName,
                result.Password,
                "Bitbucket");

            if (result.CredentialSaved == CredentialSaveOption.Selected)
            {
                CredentialManager.WriteCredential(options.ServerUri,
                    result.UserName,
                    result.Password,
                    CredentialPersistence.LocalMachine);
            }

            return credentials;
        }
    }
}