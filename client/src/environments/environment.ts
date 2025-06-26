export const environment = {
  production: false,
  apiUrl: (window as any)["env"]["apiUrl"] || '/api',
  firebase: {
    projectId: (window as any)["env"]["firebaseProjectId"] || 'jullius-financas',
    appId: (window as any)["env"]["firebaseAppId"] || '1:60405400874:web:32afb00531eca989cc0a53',
    storageBucket: (window as any)["env"]["firebaseStorageBucket"] || 'jullius-financas.firebasestorage.app',
    apiKey: (window as any)["env"]["firebaseApiKey"] || 'AIzaSyAXQ-JOF9Np_qcYpBj7Etjzwn5INE-PvEg',
    authDomain: (window as any)["env"]["firebaseAuthDomain"] || 'jullius-financas.firebaseapp.com',
    messagingSenderId: (window as any)["env"]["firebaseMessagingSenderId"] || '60405400874',
  }
};
