import { Routes } from '@angular/router';
import { adminGuard, employeeGuard } from './guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/landing/landing').then(m => m.LandingComponent)
  },
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
    path: 'admin/departments',
    loadComponent: () =>
      import('./pages/admin/departments/departments').then(m => m.DepartmentsPage),
    canActivate: [adminGuard]
  },
  {
    path: 'employee/profile',
    loadComponent: () =>
      import('./pages/employee/profile/profile').then(m => m.EmployeeProfile),
    canActivate: [employeeGuard]
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./pages/auth/forgot-password/forgot-password').then(m => m.ForgotPassword)
  },
  { path: '**', redirectTo: '' }
];
