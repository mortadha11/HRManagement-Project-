import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HrApi, Leave, Employee } from '../../../services/hr-api';
import { AuthService } from '../../../services/auth.service';
import { inject } from '@angular/core';

@Component({
  selector: 'app-admin-leaves',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  templateUrl: './leaves.html',
  styleUrls: ['./leaves.scss']
})
export class Leaves implements OnInit {
  leaves = signal<Leave[]>([]);
  employees = signal<Employee[]>([]);
  isLoading = signal(true);
  
  // Filters
  filterStatus = signal<string>('Pending');
  filterDepartment = signal<string>('All');
  filterEmployeeName = signal<string>('');

  auth = inject(AuthService);
  currentUser = this.auth.currentUser;
  isAdmin = this.auth.isAdmin;

  logout() {
    this.auth.logout();
  }

  departmentNames = computed(() => {
    // Extract unique department names from employees
    const deps = this.employees()
      .map(e => e.departmentName)
      .filter((v, i, a) => v && a.indexOf(v) === i);
    return deps as string[];
  });

  filteredLeaves = computed(() => {
    let result = this.leaves();

    // Filter by Status
    if (this.filterStatus() !== 'All') {
      result = result.filter(l => l.status === this.filterStatus());
    }

    // Filter by Employee Name Search
    if (this.filterEmployeeName().trim() !== '') {
      const search = this.filterEmployeeName().toLowerCase();
      result = result.filter(l => l.employeeName?.toLowerCase().includes(search));
    }

    // Filter by Department (Requires resolving Employee -> Department)
    if (this.filterDepartment() !== 'All') {
      // Find employee IDs that belong to the selected department
      const validEmpIds = this.employees()
        .filter(e => e.departmentName === this.filterDepartment())
        .map(e => e.id);
      
      result = result.filter(l => validEmpIds.includes(l.employeeId));
    }

    return result;
  });

  isProcessing = signal<number | null>(null);

  constructor(private api: HrApi) {}

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.isLoading.set(true);
    
    // Load Leaves
    this.api.getLeaves().subscribe({
      next: (data) => {
        this.leaves.set(data);
      },
      error: (err) => console.error(err)
    });

    // Load Employees (for mapping departments and search metadata)
    this.api.getEmployees().subscribe({
      next: (data) => {
        this.employees.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.isLoading.set(false);
      }
    });
  }

  updateStatus(id: number, status: string) {
    if (!confirm(`Are you sure you want to ${status.toLowerCase()} this leave request?`)) return;

    this.isProcessing.set(id);
    this.api.updateLeaveStatus(id, status).subscribe({
      next: () => {
        this.isProcessing.set(null);
        this.loadData(); // Re-fetch to get updated state and moderator logs
      },
      error: (err: any) => {
        this.isProcessing.set(null);
        alert(err.error?.message || `Error attempting to ${status.toLowerCase()} leave`);
      }
    });
  }
}
