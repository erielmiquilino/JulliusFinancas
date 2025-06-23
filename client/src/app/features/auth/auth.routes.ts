import { Routes } from '@angular/router';
import { noAuthGuard } from '../../core/auth/guards/no-auth.guard';

export const authRoutes: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () => import('./components/login/login.component')
      .then(c => c.LoginComponent),
    canActivate: [noAuthGuard],
    title: 'Login - Jullius Finanças'
  },
  {
    path: 'forgot-password',
    loadComponent: () => import('./components/forgot-password/forgot-password.component')
      .then(c => c.ForgotPasswordComponent),
    canActivate: [noAuthGuard],
    title: 'Recuperar Senha - Jullius Finanças'
  }
];
