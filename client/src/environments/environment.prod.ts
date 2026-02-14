export const environment = {
  production: true,
  apiUrl: (window as any)["env"]["apiUrl"] || '',
  firebase: {
    projectId: (window as any)["env"]["firebaseProjectId"] || 'your-production-project-id',
    appId: (window as any)["env"]["firebaseAppId"] || 'your-production-app-id',
    storageBucket: (window as any)["env"]["firebaseStorageBucket"] || 'your-production-storage-bucket',
    apiKey: (window as any)["env"]["firebaseApiKey"] || 'your-production-api-key',
    authDomain: (window as any)["env"]["firebaseAuthDomain"] || 'your-production-auth-domain',
    messagingSenderId: (window as any)["env"]["firebaseMessagingSenderId"] || 'your-production-messaging-sender-id',
  }
};
