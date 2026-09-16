To use the Google Drive integration feature:

1.  Go to the Google Cloud Console (https://console.cloud.google.com/).
2.  Create a new project.
3.  Enable the **Google Drive API**.
4.  Go to **Credentials** -> **Create Credentials** -> **OAuth client ID**.
5.  Choose **Desktop app** (or Web application if you configure redirect URIs correctly, but for this service account/simple integration, Desktop/Other is easiest for token generation, OR use Service Account).
    *   *Note: The code currently uses `GoogleWebAuthorizationBroker` which is designed for installed applications or local runs where a browser can open. For a web server, you typically use a Service Account or a specific Web flow.*
    *   **Recommendation**: Use a Service Account for server-side uploads without user interaction, OR if this is for an admin to log in with their own Google account, the current setup works if run locally.

    *   If using **Web Client ID** (which you currently are):
        1.  **CRITICAL ISSUE**: The current code uses `GoogleWebAuthorizationBroker` which launches a local server on a RANDOM port (e.g., `http://127.0.0.1:52341/authorize/`).
        2.  Google "Web" Clients require **EXACT** URI matching. You cannot register random ports.
        3.  **SOLUTION 1 (Recommended)**: Create a **Desktop** app credential instead of Web. It allows any localhost port.
        4.  **SOLUTION 2 (Workaround)**: 
            - Go to Console > Credentials > Edit your Web Client.
            - Add `http://127.0.0.1` and `http://localhost` to **Authorized redirect URIs**.
            - NOTE: This often FAILS because Google doesn't allow port wildcards for Web types.
        5.  **SOLUTION 3 (Code Hack)**:
            - Edit `credentials.json` and change the key `"web"` to `"installed"`.
            - This tricks the library into using "Desktop" validation mode, which works with random localhost ports.

    *   **To configure Redirect URIs in Console:**
        1.  Go to https://console.cloud.google.com/apis/credentials
        2.  Click the pencil icon next to your OAuth 2.0 Client ID.
        3.  Scroll to **Authorized redirect URIs**.
        4.  Click **ADD URI**.
        5.  Enter: `http://localhost`
        6.  Enter: `http://127.0.0.1`
        7.  Click **SAVE**.

    *   If using a **Service Account**:
        1. Create Service Account.
        2. Download JSON key.
        3. Rename to `credentials.json`.
        4. Update `GoogleDriveService.cs` to use `GoogleCredential.FromStream(stream).CreateScoped(Scopes)`.
    
    *   If using **OAuth Client ID** (Current Code):
        1. Download the JSON.
        2. Rename it to `credentials.json`.
        3. Place it in the root folder of the project (`d:\Project\DokterAbdurrahman\`).
        4. When you run the app and try to upload/list for the first time, it might try to open a browser on the server. This works fine for local development (`localhost`).

6.  Restart the application.

**Default Admin Login:**
Username: admin
Password: adminpassword

Client ID:
254098568256-lv0k1ddh9gudcn6oagndpgdpre37oi7c.apps.googleusercontent.com

Client secret:
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
