import { ApplicationConfig, LOCALE_ID } from '@angular/core';
import { provideRouter } from '@angular/router';
import { routes } from './app.routes';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './core/auth/interceptors/auth.interceptor';
import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { provideBrazilianDateAdapter } from './shared/date/brazilian-date-adapter';

registerLocaleData(localePt);

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),
    provideAnimations(),
    provideHttpClient(withInterceptors([authInterceptor])),
    { provide: LOCALE_ID, useValue: 'pt-BR' },
    ...provideBrazilianDateAdapter()
  ]
};
