import { Component, OnInit, signal, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HrApi, Department } from '../../../services/hr-api';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-departments',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './departments.html',
  styleUrl: './departments.scss'
})
export class DepartmentsPage implements OnInit {

  departments  = signal<Department[]>([]);
  isLoading    = signal(true);
  errorMsg     = '';
  successMsg   = '';

  // ── Form state ──────────────────────────────────────────
  showForm     = false;
  editingId: number | null = null;

  form = { name: '', description: '' };

  isSaving     = false;
  deleteConfirmId: number | null = null;

  get isAdmin(): boolean { return this.auth.isAdmin; }
  get currentUser() { return this.auth.currentUser; }
  logout() { this.auth.logout(); }

  constructor(
    private api:  HrApi,
    private auth: AuthService,
    private cdr:  ChangeDetectorRef
  ) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.isLoading.set(true);
    this.api.getDepartments().subscribe({
      next: (data) => {
        this.departments.set(data);
        this.isLoading.set(false);
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMsg = 'Failed to load departments.';
        this.isLoading.set(false);
        this.cdr.detectChanges();
      }
    });
  }

  openCreate(): void {
    this.editingId   = null;
    this.form        = { name: '', description: '' };
    this.showForm    = true;
    this.successMsg  = '';
    this.errorMsg    = '';
  }

  openEdit(dept: Department): void {
    this.editingId  = dept.id;
    this.form       = { name: dept.name, description: dept.description ?? '' };
    this.showForm   = true;
    this.successMsg = '';
    this.errorMsg   = '';
  }

  save(): void {
    if (!this.form.name.trim()) {
      this.errorMsg = 'Department name is required.';
      return;
    }

    this.isSaving = true;
    this.errorMsg = '';

    const payload: Department = {
      id:          this.editingId ?? 0,
      name:        this.form.name.trim(),
      description: this.form.description.trim() || null
    };

    const action$ = this.editingId
      ? this.api.updateDepartment(this.editingId, payload)
      : this.api.createDepartment(payload);

    action$.subscribe({
      next: () => {
        this.successMsg = this.editingId
          ? 'Department updated successfully.'
          : 'Department created successfully.';
        this.showForm = false;
        this.isSaving = false;
        this.load();
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMsg = 'Failed to save department.';
        this.isSaving = false;
        this.cdr.detectChanges();
      }
    });
  }

  cancelForm(): void {
    this.showForm  = false;
    this.editingId = null;
    this.errorMsg  = '';
  }

  confirmDelete(id: number): void {
    this.deleteConfirmId = id;
  }

  cancelDelete(): void {
    this.deleteConfirmId = null;
  }

  deleteDept(id: number): void {
    this.api.deleteDepartment(id).subscribe({
      next: () => {
        this.successMsg      = 'Department deleted.';
        this.deleteConfirmId = null;
        this.load();
        this.cdr.detectChanges();
      },
      error: () => {
        this.errorMsg        = 'Cannot delete — department may have employees assigned.';
        this.deleteConfirmId = null;
        this.cdr.detectChanges();
      }
    });
  }

  clearMessages(): void {
    this.successMsg = '';
    this.errorMsg   = '';
  }
}
