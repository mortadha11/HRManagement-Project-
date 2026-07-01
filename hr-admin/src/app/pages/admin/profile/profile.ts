import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { Employee, HrApi } from '../../../services/hr-api';

@Component({
  selector: 'app-admin-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './profile.html',
  styleUrl: './profile.scss'
})
export class AdminProfile implements OnInit {
  employee = signal<Employee | null>(null);
  isLoading = signal(true);
  isSaving = signal(false);
  isChangingPassword = signal(false);
  error = signal('');
  success = signal('');
  passwordError = signal('');
  passwordSuccess = signal('');

  profileForm: ReturnType<AdminProfile['buildProfileForm']>;
  passwordForm: ReturnType<AdminProfile['buildPasswordForm']>;

  constructor(
    public auth: AuthService,
    private api: HrApi,
    private fb: FormBuilder,
    private router: Router
  ) {
    this.profileForm = this.buildProfileForm();
    this.passwordForm = this.buildPasswordForm();
  }

  private buildProfileForm() {
    return this.fb.nonNullable.group({
      firstName: ['', [Validators.required, Validators.maxLength(80)]],
      lastName: ['', [Validators.required, Validators.maxLength(80)]],
      email: ['', [Validators.required, Validators.email]],
      phone: [''],
    });
  }

  private buildPasswordForm() {
    return this.fb.nonNullable.group({
      currentPassword: ['', [Validators.required, Validators.minLength(6)]],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', [Validators.required, Validators.minLength(6)]],
    });
  }

  ngOnInit(): void {
    const empId = this.auth.currentUser?.employeeId;
    if (!empId) {
      this.router.navigate(['/login']);
      return;
    }

    this.api.getEmployeeById(empId).subscribe({
      next: (emp) => {
        this.employee.set(emp);
        this.profileForm.reset({
          firstName: emp.firstName,
          lastName: emp.lastName,
          email: emp.email,
          phone: emp.phone ?? '',
        });
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Impossible de charger votre profil.');
        this.isLoading.set(false);
      }
    });
  }

  save(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }

    const emp = this.employee();
    if (!emp) return;

    this.isSaving.set(true);
    this.error.set('');
    this.success.set('');

    const { firstName, lastName, email, phone } = this.profileForm.getRawValue();
    this.api.updateEmployeeProfile(emp.id, {
      firstName,
      lastName,
      email,
      phone: phone || null
    }).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.employee.set({
          ...emp,
          firstName,
          lastName,
          email,
          phone: phone || null
        });
        this.auth.updateSessionUser({ fullName: `${firstName} ${lastName}` });
        this.success.set('Profil mis a jour avec succes !');
      },
      error: () => {
        this.isSaving.set(false);
        this.error.set('Erreur lors de la mise a jour.');
      }
    });
  }

  changePassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }

    const { currentPassword, newPassword, confirmPassword } = this.passwordForm.getRawValue();
    if (newPassword !== confirmPassword) {
      this.passwordError.set('Les mots de passe ne correspondent pas.');
      return;
    }

    this.isChangingPassword.set(true);
    this.passwordError.set('');
    this.passwordSuccess.set('');

    this.api.changePassword({ currentPassword, newPassword }).subscribe({
      next: (res) => {
        this.isChangingPassword.set(false);
        this.passwordSuccess.set(res.message);
        this.passwordForm.reset({
          currentPassword: '',
          newPassword: '',
          confirmPassword: '',
        });
      },
      error: (err) => {
        this.isChangingPassword.set(false);
        this.passwordError.set(err.error?.message ?? 'Erreur lors du changement de mot de passe.');
      }
    });
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }

  get user() {
    return this.auth.currentUser;
  }
  
  get isAdmin() {
    return this.auth.isAdmin;
  }

  initials(emp: Employee) {
    return `${emp.firstName[0]}${emp.lastName[0]}`.toUpperCase();
  }
}
