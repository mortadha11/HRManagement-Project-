import { Routes } from '@angular/router';
import { adminGuard, employeeGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./pages/auth/login/login').then(m => m.LoginComponent)
  },
  {
    path: 'admin/dashboard',
    loadComponent: () =>
      import('./pages/admin/dashboard/dashboard').then(m => m.Dashboard),
    canActivate: [adminGuard]
  },
  {
    path: 'employee/profile',
    loadComponent: () =>
      import('./pages/employee/profile/profile').then(m => m.EmployeeProfile),
    canActivate: [employeeGuard]
  },
  { path: '',   redirectTo: 'login', pathMatch: 'full' },
  { path: '**', redirectTo: 'login' }
];