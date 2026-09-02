import { NgModule, APP_INITIALIZER } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { CommonModule } from '@angular/common';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './core/auth/interceptors/auth.interceptor';
import { AuthService } from './core/auth/services/auth.service';
import { RouterModule } from '@angular/router';
import { registerLocaleData } from '@angular/common';
import localePt from '@angular/common/locales/pt';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';

import { provideBrazilianDateAdapter } from './shared/date/brazilian-date-adapter';

import { AppComponent } from './app.component';
import { routes } from './app.routes';
import { HeaderComponent } from './layout/header/header.component';
import { SideMenuComponent } from './layout/side-menu/side-menu.component';


registerLocaleData(localePt);

@NgModule({
  declarations: [
  ],
  imports: [
    AppComponent,
    BrowserModule,
    CommonModule,
    BrowserAnimationsModule,
    RouterModule.forRoot(routes),
    MatToolbarModule,
    MatIconModule,
    MatSidenavModule,
    MatListModule,
    HeaderComponent,
    SideMenuComponent
  ],
  providers: [
    provideHttpClient(withInterceptors([authInterceptor])),
    {
      provide: APP_INITIALIZER,
      useFactory: (authService: AuthService) => () => authService.whenReady(),
      deps: [AuthService],
      multi: true
    },
    ...provideBrazilianDateAdapter()
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
