import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { filter, take, map, tap } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

export const authGuard = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.authState$.pipe(
    filter(state => !state.isLoading),
    take(1),
    map(state => state.isAuthenticated),
    tap(isAuthenticated => {
      if (!isAuthenticated) {
        router.navigate(['/auth/login']);
      }
    })
  );
};
