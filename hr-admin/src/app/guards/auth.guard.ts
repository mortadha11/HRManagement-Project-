import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn) return true;
  return router.createUrlTree(['/login'], {
    queryParams: { reason: 'sign-in-again' }
  });
};

export const adminGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  // Only STRICT Admins can view this! Managers are redirected.
  if (auth.isLoggedIn && auth.isAdmin) return true;
  if (auth.isLoggedIn && auth.isManager) return router.createUrlTree(['/manager/dashboard']);
  if (auth.isLoggedIn) {
    return router.createUrlTree(['/employee/profile']);
  }
  return router.createUrlTree(['/login'], {
    queryParams: { reason: 'sign-in-again' }
  });
};

export const managerGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn && auth.isManager) return true;
  if (auth.isLoggedIn && auth.isAdmin) return router.createUrlTree(['/admin/dashboard']);
  if (auth.isLoggedIn) {
    return router.createUrlTree(['/employee/profile']);
  }
  return router.createUrlTree(['/login'], {
    queryParams: { reason: 'sign-in-again' }
  });
};

export const employeeGuard: CanActivateFn = () => {
  const auth   = inject(AuthService);
  const router = inject(Router);
  if (!auth.isLoggedIn) {
    return router.createUrlTree(['/login'], {
      queryParams: { reason: 'sign-in-again' }
    });
  }
  return true;
};
