import { Routes } from '@angular/router';
import { authGuard } from './core/auth/guards/auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.routes')
      .then(m => m.authRoutes)
  },
  {
    path: 'dashboard',
    loadChildren: () => import('./features/dashboard/dashboard.routes')
      .then(m => m.dashboardRoutes),
    canActivate: [authGuard]
  },
  {
    path: 'transactions',
    loadChildren: () => import('./features/financial-transaction/financial-transaction.module')
      .then(m => m.FinancialTransactionModule),
    canActivate: [authGuard]
  },
  {
    path: 'cards',
    loadChildren: () => import('./features/cards/card.module')
      .then(m => m.CardModule),
    canActivate: [authGuard]
  },
  {
    path: 'categories',
    loadChildren: () => import('./features/categories/categories.module')
      .then(m => m.CategoriesModule),
    canActivate: [authGuard]
  },
  {
    path: 'budgets',
    loadChildren: () => import('./features/budgets/budgets.module')
      .then(m => m.BudgetsModule),
    canActivate: [authGuard]
  },
  {
    path: 'overdue-accounts',
    loadChildren: () => import('./features/overdue-accounts/overdue-accounts.module')
      .then(m => m.OverdueAccountsModule),
    canActivate: [authGuard]
  },
  {
    path: 'settings',
    loadChildren: () => import('./features/settings/settings.routes')
      .then(m => m.settingsRoutes),
    canActivate: [authGuard]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
