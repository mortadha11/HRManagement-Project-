import { CommonModule } from '@angular/common';
import { Component, ElementRef, OnInit, ViewChild, computed, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Observable, forkJoin } from 'rxjs';
import { Contract, Department, Employee, HrApi, Leave } from '../../../services/hr-api';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  @ViewChild('employeeFormPanel') employeeFormPanel?: ElementRef<HTMLElement>;
  @ViewChild('firstNameInput')    firstNameInput?:    ElementRef<HTMLInputElement>;

  // ── State ─────────────────────────────────────────────
  employees   = signal<Employee[]>([]);
  departments = signal<Department[]>([]);
  contracts   = signal<Contract[]>([]);
  leaves      = signal<Leave[]>([]);
  searchTerm      = signal('');
  editingEmployee = signal<Employee | null>(null);
  formFlash   = signal(false);
  isSaving    = signal(false);
  isLoading   = signal(true);
  error       = signal('');

  // ── today for the template ────────────────────────────
  today = new Date();

  employeeForm: ReturnType<Dashboard['createEmployeeForm']>;

  constructor(
    private readonly api:    HrApi,
    private readonly fb:     FormBuilder,
    private readonly auth:   AuthService,
    private readonly router: Router
  ) {
    this.employeeForm = this.createEmployeeForm();
  }

  // ── Auth helpers ──────────────────────────────────────
  get currentUser() { return this.auth.currentUser; }
  get isAdmin()     { return this.auth.isAdmin; }
  get isManager()   { return this.auth.isManager; }

  // ── Form factory ──────────────────────────────────────
  private createEmployeeForm() {
    return this.fb.nonNullable.group({
      firstName:    ['', [Validators.required, Validators.maxLength(80)]],
      lastName:     ['', [Validators.required, Validators.maxLength(80)]],
      email:        ['', [Validators.required, Validators.email]],
      phone:        [''],
      hireDate:     [this.todayInput(), Validators.required],
      salary:       [0],
      departmentId: [null as number | null],
      jobTitle:     [''],
      jobLevel:     ['' as '' | 'Junior' | 'Mid' | 'Senior' | 'Lead' | 'Director'],
      managerId:    [null as number | null],
      isActive:     [true],
    });
  }

  // ── Computed ──────────────────────────────────────────
  filteredEmployees = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    if (!term) return this.employees();
    return this.employees().filter(e => {
      const name = `${e.firstName} ${e.lastName}`.toLowerCase();
      return name.includes(term)
          || e.email.toLowerCase().includes(term)
          || (e.departmentName ?? '').toLowerCase().includes(term);
    });
  });

  pendingLeaves   = computed(() => this.leaves().filter(l => l.status.toLowerCase() === 'pending'));
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
      count:      this.employees().filter(e => e.departmentId === d.id).length,
      percent:    Math.round((this.employees().filter(e => e.departmentId === d.id).length / total) * 100),
      colorClass: ['blue', 'purple', 'orange', 'green'][i % 4],
    }));
  });

  // ── Lifecycle ─────────────────────────────────────────
  ngOnInit(): void { this.loadDashboard(); }

  loadDashboard(): void {
    this.isLoading.set(true);
    this.error.set('');

    if (!this.isAdmin && !this.isManager) {
      this.loadEmployeeProfile();
      return;
    }

    forkJoin({
      employees:   this.api.getEmployees(),
      departments: this.api.getDepartments(),
      contracts:   this.api.getContracts(),
      leaves:      this.api.getLeaves(),
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

  private loadEmployeeProfile(): void {
    const empId = this.auth.currentUser?.employeeId;
    if (!empId) return;
    this.api.getEmployeeById(empId).subscribe({
      next: (emp) => {
        this.employees.set([emp]);
        this.isLoading.set(false);
        this.startEdit(emp);
      },
      error: () => {
        this.error.set('Impossible de charger votre profil.');
        this.isLoading.set(false);
      }
    });
  }

  // ── Form actions ──────────────────────────────────────
  startCreate(): void {
    if (!this.isAdmin && !this.isManager) return;
    this.editingEmployee.set(null);
    this.employeeForm.reset({
      firstName: '', lastName: '', email: '', phone: '',
      hireDate: this.todayInput(), salary: 0,
      departmentId: null, jobTitle: '', jobLevel: '',
      managerId: null, isActive: true,
    });
    this.setFormPermissions();
    this.focusEmployeeForm();
  }

  startEdit(employee: Employee): void {
    this.editingEmployee.set(employee);
    this.employeeForm.reset({
      firstName:    employee.firstName,
      lastName:     employee.lastName,
      email:        employee.email,
      phone:        employee.phone ?? '',
      hireDate:     this.toInputDate(employee.hireDate),
      salary:       employee.salary ?? 0,
      departmentId: employee.departmentId ?? null,
      jobTitle:     employee.jobTitle ?? '',
      jobLevel:     (employee.jobLevel as any) ?? '',
      managerId:    employee.managerId ?? null,
      isActive:     employee.isActive,
    });
    this.setFormPermissions();
    this.focusEmployeeForm();
  }

  private setFormPermissions(): void {
    // Enable everything first
    this.employeeForm.enable();
    if (this.isAdmin || this.isManager) return;

    // Simple employee → only phone & email editable
    const c = this.employeeForm.controls;
    c.firstName.disable();
    c.lastName.disable();
    c.hireDate.disable();
    c.salary.disable();
    c.departmentId.disable();
    c.jobTitle.disable();
    c.jobLevel.disable();
    c.managerId.disable();
    c.isActive.disable();
  }

  saveEmployee(): void {
    if (this.employeeForm.invalid) {
      this.employeeForm.markAllAsTouched();
      this.error.set('Veuillez remplir tous les champs obligatoires.');
      return;
    }

    const existing  = this.editingEmployee();
    const formValue = this.employeeForm.getRawValue();
    this.isSaving.set(true);

    // Simple employee → update profile only
    if (!this.isAdmin && !this.isManager && existing) {
      this.api.updateEmployeeProfile(existing.id, {
        email: formValue.email,
        phone: formValue.phone ?? null,
      }).subscribe({
        next: () => { this.isSaving.set(false); this.loadDashboard(); },
        error: () => { this.error.set('Mise à jour échouée.'); this.isSaving.set(false); }
      });
      return;
    }

    const payload = {
      ...formValue,
      salary:       Number(formValue.salary) || null,
      departmentId: formValue.departmentId ? Number(formValue.departmentId) : null,
      managerId:    formValue.managerId     ? Number(formValue.managerId)    : null,
      hireDate:     new Date(formValue.hireDate).toISOString(),
      createdAt:    existing?.createdAt ?? new Date().toISOString(),
    };

    const request: Observable<Employee | void> = existing
      ? this.api.updateEmployee(existing.id, { ...payload, id: existing.id })
      : this.api.createEmployee(payload);

    request.subscribe({
      next: () => { this.isSaving.set(false); this.startCreate(); this.loadDashboard(); },
      error: () => { this.error.set('Saving failed. Check required fields and API connection.'); this.isSaving.set(false); },
    });
  }

  deleteEmployee(employee: Employee): void {
    if (!this.isAdmin) return;
    if (!confirm(`Delete ${employee.firstName} ${employee.lastName}?`)) return;
    this.api.deleteEmployee(employee.id).subscribe({
      next: () => this.loadDashboard(),
      error: () => this.error.set('Delete failed.'),
    });
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }

  // ── Helpers ───────────────────────────────────────────
  contractType(employeeId: number): string {
    return this.contracts().find(c => c.employeeId === employeeId)?.type ?? 'None';
  }

  initials(employee: Employee): string {
    return `${employee.firstName.charAt(0)}${employee.lastName.charAt(0)}`.toUpperCase();
  }

  private todayInput = (): string => this.toInputDate(new Date().toISOString());
  private toInputDate = (value: string): string => new Date(value).toISOString().slice(0, 10);

  private focusEmployeeForm(): void {
    this.formFlash.set(true);
    setTimeout(() => {
      this.employeeFormPanel?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
      this.firstNameInput?.nativeElement.focus();
    });
    setTimeout(() => this.formFlash.set(false), 1200);
  }
}