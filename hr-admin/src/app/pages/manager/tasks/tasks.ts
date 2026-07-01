import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { EmployeeTask, HrApi } from '../../../services/hr-api';

@Component({
  selector: 'app-manager-tasks',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './tasks.html',
  styleUrl: './tasks.scss'
})
export class ManagerTasks implements OnInit {
  assignedTasks = signal<EmployeeTask[]>([]);
  createdTasks = signal<EmployeeTask[]>([]);
  subordinates = signal<{ id: number, fullName: string }[]>([]);

  isLoading = signal(true);
  error = signal('');
  
  showCreateModal = signal(false);
  isSaving = signal(false);
  taskForm: ReturnType<ManagerTasks['buildForm']>;

  constructor(
    public auth: AuthService,
    private api: HrApi,
    private fb: FormBuilder,
    private router: Router
  ) {
    this.taskForm = this.buildForm();
  }

  get user() {
    return this.auth.currentUser;
  }

  get isManager() {
    return this.auth.isManager || this.auth.isAdmin;
  }

  private buildForm() {
    return this.fb.nonNullable.group({
      title: ['', [Validators.required, Validators.maxLength(200)]],
      description: [''],
      dueDate: [''],
      priorityLevel: ['Medium', [Validators.required]],
      employeeId: [null as number | null, [Validators.required]]
    });
  }

  ngOnInit(): void {
    if (!this.user) {
      this.router.navigate(['/login']);
      return;
    }
    this.loadData();
  }

  loadData(): void {
    this.isLoading.set(true);
    let loaded = 0;
    const total = this.isManager ? 3 : 1;

    this.api.getAssignedTasks().subscribe({
      next: (tasks) => {
        this.assignedTasks.set(tasks);
        loaded++;
        if (loaded === total) this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });

    if (this.isManager) {
      this.api.getCreatedTasks().subscribe({
        next: (tasks) => {
          tasks.sort((a, b) => this.getPriorityScore(a.priorityLevel) - this.getPriorityScore(b.priorityLevel));
          this.createdTasks.set(tasks);
          loaded++;
          if (loaded === total) this.isLoading.set(false);
        }
      });
      // Fetch subordinates to populate the dropdown
      this.api.getEmployeeById(this.user!.employeeId!).subscribe({
        next: (emp) => {
          this.subordinates.set(emp.subordinates || []);
          loaded++;
          if (loaded === total) this.isLoading.set(false);
        }
      });
    }
  }

  private getPriorityScore(pLevel: string | undefined): number {
    if (pLevel === 'High') return 1;
    if (pLevel === 'Medium') return 2;
    return 3;
  }

  updateTaskStatus(taskId: number, event: Event) {
    const status = (event.target as HTMLSelectElement).value;
    this.api.updateTaskStatus(taskId, status).subscribe({
      next: () => this.loadData(),
      error: () => alert('Failed to update status')
    });
  }

  deleteTask(taskId: number) {
    if(!confirm('Are you sure you want to delete this task?')) return;
    this.api.deleteTask(taskId).subscribe({
      next: () => this.loadData(),
      error: () => alert('Failed to delete task')
    });
  }

  startCreate() {
    this.taskForm.reset();
    this.showCreateModal.set(true);
  }

  saveTask() {
    if (this.taskForm.invalid) {
      this.taskForm.markAllAsTouched();
      return;
    }
    this.isSaving.set(true);
    const formVal = this.taskForm.getRawValue();
    this.api.createTask({
      title: formVal.title,
      description: formVal.description || null,
      dueDate: formVal.dueDate ? new Date(formVal.dueDate).toISOString() : null,
      priorityLevel: formVal.priorityLevel,
      employeeId: Number(formVal.employeeId)
    }).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.showCreateModal.set(false);
        this.loadData();
      },
      error: (err: any) => {
        this.isSaving.set(false);
        alert(err.error?.message || 'Error saving task');
      }
    });
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
