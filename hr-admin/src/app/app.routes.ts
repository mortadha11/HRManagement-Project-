import { Routes } from '@angular/router';
import { adminGuard, employeeGuard, managerGuard } from './guards/auth.guard';

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
    path: 'admin/profile',
    loadComponent: () =>
      import('./pages/admin/profile/profile').then(m => m.AdminProfile),
    canActivate: [adminGuard]
  },
  {
    path: 'manager/dashboard',
    loadComponent: () =>
      import('./pages/manager/tasks/tasks').then(m => m.ManagerTasks),
    canActivate: [managerGuard]
  },
  {
    path: 'manager/profile',
    loadComponent: () =>
      import('./pages/manager/profile/profile').then(m => m.ManagerProfile),
    canActivate: [managerGuard]
  },
  {
    path: 'manager/tasks',
    loadComponent: () =>
      import('./pages/manager/tasks/tasks').then(m => m.ManagerTasks),
    canActivate: [managerGuard]
  },
  {
    path: 'employee/profile',
    loadComponent: () =>
      import('./pages/employee/profile/profile').then(m => m.EmployeeProfile),
    canActivate: [employeeGuard]
  },
  {
    path: 'employee/tasks',
    loadComponent: () =>
      import('./pages/employee/tasks/tasks').then(m => m.TasksComponent),
    canActivate: [employeeGuard]
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./pages/auth/forgot-password/forgot-password').then(m => m.ForgotPassword)
  },
  {
    path: 'admin/contracts',
    loadComponent: () =>
      import('./pages/admin/contracts/contracts').then(m => m.Contracts),
    canActivate: [adminGuard]
  },
  {
    path: 'admin/leaves',
    loadComponent: () =>
      import('./pages/admin/leaves/leaves').then(m => m.Leaves),
    canActivate: [adminGuard]
  },
  {
    path: 'employee/leaves',
    loadComponent: () =>
      import('./pages/employee/leaves/leaves').then(m => m.Leaves),
    canActivate: [employeeGuard]
  },
  { path: '**', redirectTo: '' }
];
