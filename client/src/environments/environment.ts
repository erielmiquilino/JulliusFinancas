export const environment = {
  production: false,
  apiUrl: (window as any)["env"]["apiUrl"] || '/api',
  firebase: {
    projectId: (window as any)["env"]["firebaseProjectId"] || 'your-project-id',
    appId: (window as any)["env"]["firebaseAppId"] || '1:123456789012:web:abcdef1234567890',
    storageBucket: (window as any)["env"]["firebaseStorageBucket"] || 'your-project-id.firebasestorage.app',
    apiKey: (window as any)["env"]["firebaseApiKey"] || 'AIzaSyEXAMPLEKEY1234567890',
    authDomain: (window as any)["env"]["firebaseAuthDomain"] || 'your-project-id.firebaseapp.com',
    messagingSenderId: (window as any)["env"]["firebaseMessagingSenderId"] || '123456789012',
  }
};
