import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Observable, forkJoin } from 'rxjs';
import {
  Contract,
  CreateEmployeeResponse,
  Department,
  Employee,
  HrApi,
  Leave,
  UserAccount
} from '../../../services/hr-api';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly namePattern = /^[A-Za-zÀ-ÿ][A-Za-zÀ-ÿ' -]{1,79}$/;
  private readonly phonePattern = /^[0-9+\s-]{6,20}$/;

  employees = signal<Employee[]>([]);
  departments = signal<Department[]>([]);
  contracts = signal<Contract[]>([]);
  leaves = signal<Leave[]>([]);
  searchTerm = signal('');
  editingEmployee = signal<Employee | null>(null);
  showEmployeeModal = signal(false);
  passwordMode = signal<'suggested' | 'manual'>('suggested');
  suggestedPassword = signal('');

  selectedEmployee = signal<Employee | null>(null);
  selectedUserAccount = signal<UserAccount | null>(null);
  showDetailPanel = signal(false);
  newPassword = signal('');
  passwordMsg = signal('');
  passwordError = signal('');
  isResettingPassword = signal(false);
  showCreateAccount = signal(false);
  newAccountUsername = signal('');
  newAccountPassword = signal('');
  newAccountRole = signal<'Employee' | 'Manager' | 'Admin'>('Employee');
  createAccountMsg = signal('');
  generatedCredentials = signal<CreateEmployeeResponse | null>(null);

  isSaving = signal(false);
  isLoading = signal(true);
  error = signal('');
  today = new Date();

  employeeForm: ReturnType<Dashboard['createEmployeeForm']>;

  constructor(
    private readonly api: HrApi,
    private readonly fb: FormBuilder,
    private readonly auth: AuthService,
    private readonly router: Router
  ) {
    this.employeeForm = this.createEmployeeForm();
  }

  get currentUser() {
    return this.auth.currentUser;
  }

  get isAdmin() {
    return this.auth.isAdmin;
  }

  get isManager() {
    return this.auth.isManager;
  }

  private createEmployeeForm() {
    return this.fb.nonNullable.group({
      firstName: ['', [Validators.required, Validators.maxLength(80), Validators.pattern(this.namePattern)]],
      lastName: ['', [Validators.required, Validators.maxLength(80), Validators.pattern(this.namePattern)]],
      email: ['', [Validators.required, Validators.email]],
      phone: ['', [Validators.pattern(this.phonePattern)]],
      hireDate: [this.todayInput(), Validators.required],
      salary: [0, [Validators.min(0), Validators.max(9999999)]],
      departmentId: [null as number | null],
      jobTitle: ['', [Validators.maxLength(80)]],
      jobLevel: ['' as '' | 'Junior' | 'Mid' | 'Senior' | 'Lead' | 'Director'],
      managerId: [null as number | null],
      role: ['Employee' as 'Employee' | 'Manager' | 'Admin', [Validators.required]],
      isActive: [true],
      password: [''],
    });
  }

  filteredEmployees = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    if (!term) return this.employees();
    return this.employees().filter(e => {
      const name = `${e.firstName} ${e.lastName}`.toLowerCase();
      return name.includes(term) || e.email.toLowerCase().includes(term) || (e.departmentName ?? '').toLowerCase().includes(term);
    });
  });

  pendingLeaves = computed(() => this.leaves().filter(l => l.status.toLowerCase() === 'pending'));
  activeContracts = computed(() => this.contracts().filter(c => !c.endDate || new Date(c.endDate) >= new Date()));

  newEmployeesThisMonth = computed(() => {
    const now = new Date();
    return this.employees().filter(e => {
      const d = e.createdAt ? new Date(e.createdAt) : null;
      return d && d.getMonth() === now.getMonth() && d.getFullYear() === now.getFullYear();
    }).length;
  });

  departmentSummaries = computed(() => {
    const total = Math.max(this.employees().length, 1);
    return this.departments().map((d, i) => ({
      ...d,
      count: this.employees().filter(e => e.departmentId === d.id).length,
      percent: Math.round((this.employees().filter(e => e.departmentId === d.id).length / total) * 100),
      colorClass: ['blue', 'purple', 'orange', 'green'][i % 4],
    }));
  });

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading.set(true);
    this.error.set('');
    forkJoin({
      employees: this.api.getEmployees(),
      departments: this.api.getDepartments(),
      contracts: this.api.getContracts(),
      leaves: this.api.getLeaves(),
    }).subscribe({
      next: ({ employees, departments, contracts, leaves }) => {
        this.employees.set(employees);
        this.departments.set(departments);
        this.contracts.set(contracts);
        this.leaves.set(leaves);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Could not load dashboard data. Make sure the .NET API is running.');
        this.isLoading.set(false);
      },
    });
  }

  openDetail(emp: Employee): void {
    this.selectedEmployee.set(emp);
    this.selectedUserAccount.set(null);
    this.showDetailPanel.set(true);
    this.newPassword.set('');
    this.passwordMsg.set('');
    this.passwordError.set('');
    this.showCreateAccount.set(false);
    this.createAccountMsg.set('');
    this.showEmployeeModal.set(false);

    this.api.getUserByEmployee(emp.id).subscribe({
      next: (account) => this.selectedUserAccount.set(account),
      error: () => this.selectedUserAccount.set(null),
    });
  }

  closeDetail(): void {
    this.showDetailPanel.set(false);
    this.selectedEmployee.set(null);
    this.selectedUserAccount.set(null);
  }

  startCreate(): void {
    this.closeDetail();
    this.editingEmployee.set(null);
    this.generatedCredentials.set(null);
    this.employeeForm.reset(this.emptyEmployeeValues());
    this.useSuggestedPassword();
    this.showEmployeeModal.set(true);
    this.error.set('');
  }

  controlHasError(controlName: string, errorName?: string): boolean {
    const control = this.employeeForm.get(controlName);
    if (!control) return false;
    if (errorName) return control.hasError(errorName) && (control.touched || control.dirty);
    return control.invalid && (control.touched || control.dirty);
  }

  startEdit(employee: Employee): void {
    this.closeDetail();
    this.editingEmployee.set(employee);
    this.generatedCredentials.set(null);
    this.passwordMode.set('suggested');
    this.employeeForm.controls.password.clearValidators();
    this.employeeForm.controls.password.updateValueAndValidity();
    this.employeeForm.reset({
      firstName: employee.firstName,
      lastName: employee.lastName,
      email: employee.email,
      phone: employee.phone ?? '',
      hireDate: this.toInputDate(employee.hireDate),
      salary: employee.salary ?? 0,
      departmentId: employee.departmentId ?? null,
      jobTitle: employee.jobTitle ?? '',
      jobLevel: (employee.jobLevel as any) ?? '',
      managerId: employee.managerId ?? null,
      role: (employee.role as any) ?? 'Employee',
      isActive: employee.isActive,
      password: '',
    });
    this.showEmployeeModal.set(true);
    this.error.set('');
  }

  closeEmployeeModal(): void {
    this.showEmployeeModal.set(false);
    this.editingEmployee.set(null);
    this.isSaving.set(false);
    this.generatedCredentials.set(null);
  }

  resetEmployeeForm(): void {
    this.generatedCredentials.set(null);
    const existing = this.editingEmployee();
    if (existing) {
      this.passwordMode.set('suggested');
      this.employeeForm.controls.password.clearValidators();
      this.employeeForm.controls.password.updateValueAndValidity();
      this.employeeForm.reset({
        firstName: existing.firstName,
        lastName: existing.lastName,
        email: existing.email,
        phone: existing.phone ?? '',
        hireDate: this.toInputDate(existing.hireDate),
        salary: existing.salary ?? 0,
        departmentId: existing.departmentId ?? null,
        jobTitle: existing.jobTitle ?? '',
        jobLevel: (existing.jobLevel as any) ?? '',
        managerId: existing.managerId ?? null,
        role: (existing.role as any) ?? 'Employee',
        isActive: existing.isActive,
        password: '',
      });
      return;
    }

    this.employeeForm.reset(this.emptyEmployeeValues());
    this.useSuggestedPassword();
  }

  useSuggestedPassword(): void {
    this.passwordMode.set('suggested');
    this.employeeForm.controls.password.clearValidators();
    this.employeeForm.controls.password.updateValueAndValidity();
    this.refreshSuggestedPassword();
  }

  useManualPassword(): void {
    this.passwordMode.set('manual');
    this.employeeForm.controls.password.setValidators([Validators.required, Validators.minLength(6)]);
    this.employeeForm.controls.password.updateValueAndValidity();
    this.employeeForm.controls.password.setValue('');
    this.employeeForm.controls.password.markAsTouched();
  }

  refreshSuggestedPassword(): void {
    const suggestion = this.buildSuggestedPassword();
    this.suggestedPassword.set(suggestion);
    this.employeeForm.controls.password.setValue(suggestion);
  }

  private buildSuggestedPassword(): string {
    const firstName = this.employeeForm.controls.firstName.value.trim().replace(/\s+/g, '');
    const lastName = this.employeeForm.controls.lastName.value.trim().replace(/\s+/g, '');
    const phone = this.employeeForm.controls.phone.value.replace(/\D/g, '');

    if (firstName.length < 2 || lastName.length < 2 || phone.length < 3) {
      return '';
    }

    return (firstName.slice(0, 2) + lastName.slice(0, 2) + phone.slice(-3)).toLowerCase();
  }

  saveEmployee(): void {
    if (this.employeeForm.invalid) {
      this.employeeForm.markAllAsTouched();
      this.error.set('Please fill in the required fields.');
      return;
    }

    const existing = this.editingEmployee();
    const formValue = this.employeeForm.getRawValue();
    this.isSaving.set(true);
    const password = formValue.password?.trim() ?? '';

    if (!existing && this.passwordMode() === 'suggested') {
      const suggested = password || this.buildSuggestedPassword();
      if (!suggested) {
        this.isSaving.set(false);
        this.error.set('Enter a phone number with at least 3 digits to generate a suggested password, or switch to manual mode.');
        return;
      }
      this.employeeForm.controls.password.setValue(suggested);
    }

    if (!existing && this.passwordMode() === 'manual' && password.length < 6) {
      this.isSaving.set(false);
      this.error.set('Manual password must contain at least 6 characters.');
      this.employeeForm.controls.password.markAsTouched();
      return;
    }

    const payload = {
      ...formValue,
      salary: Number(formValue.salary) || null,
      departmentId: formValue.departmentId ? Number(formValue.departmentId) : null,
      managerId: formValue.managerId ? Number(formValue.managerId) : null,
      hireDate: new Date(formValue.hireDate).toISOString(),
      createdAt: existing?.createdAt ?? new Date().toISOString(),
      password: existing ? null : (this.employeeForm.controls.password.value || null),
    };

    const request: Observable<CreateEmployeeResponse | void> = existing
      ? this.api.updateEmployee(existing.id, { ...payload, id: existing.id })
      : this.api.createEmployee(payload);

    request.subscribe({
      next: (result) => {
        this.isSaving.set(false);
        this.loadDashboard();

        if (!existing && result) {
          this.generatedCredentials.set(result as CreateEmployeeResponse);
          this.employeeForm.reset(this.emptyEmployeeValues());
          this.passwordMode.set('suggested');
          this.suggestedPassword.set('');
          this.editingEmployee.set(null);
          return;
        }

        this.closeEmployeeModal();
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'Saving failed.');
        this.isSaving.set(false);
      },
    });
  }

  generatePasswordDraft(): void {
    this.newPassword.set(this.makeTemporaryPassword());
  }

  resetPassword(): void {
    const emp = this.selectedEmployee();
    if (!emp) return;

    const draft = this.newPassword().trim();
    this.isResettingPassword.set(true);
    this.passwordError.set('');
    this.passwordMsg.set('');

    this.api.resetEmployeePassword(emp.id, draft || null).subscribe({
      next: (res) => {
        this.passwordMsg.set(res.message);
        this.newPassword.set(res.temporaryPassword);
        this.isResettingPassword.set(false);
      },
      error: () => {
        this.passwordError.set('Password reset failed.');
        this.isResettingPassword.set(false);
      }
    });
  }

  createAccount(): void {
    const emp = this.selectedEmployee();
    if (!emp) return;
    const username = this.newAccountUsername().trim();
    const password = this.newAccountPassword().trim();
    const role = this.newAccountRole();
    if (!username || password.length < 6) {
      this.createAccountMsg.set('Username and password (min 6 chars) are required.');
      return;
    }

    this.api.createAccount(emp.id, username, password, role).subscribe({
      next: (res) => {
        this.createAccountMsg.set(res.message);
        this.showCreateAccount.set(false);
        this.api.getUserByEmployee(emp.id).subscribe({
          next: (account) => this.selectedUserAccount.set(account)
        });
      },
      error: (err) => {
        this.createAccountMsg.set(err.error?.message ?? 'Error while creating the account.');
      }
    });
  }

  deleteEmployee(employee: Employee): void {
    if (!this.isAdmin) return;
    if (!confirm(`Delete ${employee.firstName} ${employee.lastName}?`)) return;
    this.api.deleteEmployee(employee.id).subscribe({
      next: () => {
        this.closeDetail();
        this.loadDashboard();
      },
      error: () => this.error.set('Delete failed.'),
    });
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login'], {
      queryParams: { reason: 'sign-in-again' },
      replaceUrl: true
    });
  }

  contractType(employeeId: number): string {
    return this.contracts().find(c => c.employeeId === employeeId)?.type ?? 'None';
  }

  initials(employee: Employee): string {
    return `${employee.firstName[0]}${employee.lastName[0]}`.toUpperCase();
  }

  private emptyEmployeeValues() {
    return {
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      hireDate: this.todayInput(),
      salary: 0,
      departmentId: null,
      jobTitle: '',
      jobLevel: '' as '' | 'Junior' | 'Mid' | 'Senior' | 'Lead' | 'Director',
      managerId: null,
      role: 'Employee' as 'Employee' | 'Manager' | 'Admin',
      isActive: true,
      password: '',
    };
  }

  private todayInput = (): string => this.toInputDate(new Date().toISOString());
  private toInputDate = (v: string): string => new Date(v).toISOString().slice(0, 10);

  private makeTemporaryPassword(length = 12): string {
    const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%';
    const values = new Uint32Array(length);
    crypto.getRandomValues(values);
    return Array.from(values, value => chars[value % chars.length]).join('');
  }
}
