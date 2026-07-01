import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HrApi, Contract, Employee } from '../../../services/hr-api';
import { AuthService } from '../../../services/auth.service';
import { inject } from '@angular/core';

@Component({
  selector: 'app-contracts',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterLink],
  templateUrl: './contracts.html',
  styleUrls: ['./contracts.scss']
})
export class Contracts implements OnInit {
  contracts = signal<Contract[]>([]);
  employees = signal<Employee[]>([]);
  isLoading = signal(true);
  
  // Filters
  filterStatus = signal<string>('All');
  filterType = signal<string>('All');

  auth = inject(AuthService);
  currentUser = this.auth.currentUser;
  isAdmin = this.auth.isAdmin;

  logout() {
    this.auth.logout();
  }

  filteredContracts = computed(() => {
    return this.contracts().filter(c => 
      (this.filterStatus() === 'All' || c.status === this.filterStatus()) &&
      (this.filterType() === 'All' || c.type === this.filterType())
    );
  });

  // Modal State
  showModal = signal(false);
  isEditing = signal(false);
  isSaving = signal(false);
  form: FormGroup;
  currentContractId: number | null = null;

  constructor(private api: HrApi, private fb: FormBuilder) {
    this.form = this.fb.group({
      employeeId: [null, Validators.required],
      type: ['CDI', Validators.required],
      position: ['', Validators.required],
      salary: [null],
      workingHours: [40],
      startDate: ['', Validators.required],
      endDate: [''],
      status: ['Active']
    });
  }

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.isLoading.set(true);
    // Load Contracts
    this.api.getContracts().subscribe({
      next: (data) => {
        this.contracts.set(data);
      },
      error: (err) => console.error(err)
    });

    // Load Employees for the dropdown
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

  openCreateModal() {
    this.isEditing.set(false);
    this.currentContractId = null;
    this.form.reset({ type: 'CDI', workingHours: 40, status: 'Active' });
    this.form.get('employeeId')?.enable(); // Allow selecting employee
    this.showModal.set(true);
  }

  openEditModal(contract: Contract) {
    this.isEditing.set(true);
    this.currentContractId = contract.id;
    this.form.patchValue({
      employeeId: contract.employeeId,
      type: contract.type,
      position: contract.position,
      salary: contract.salary,
      workingHours: contract.workingHours,
      startDate: contract.startDate ? contract.startDate.substring(0, 10) : '',
      endDate: contract.endDate ? contract.endDate.substring(0, 10) : '',
      status: contract.status
    });
    this.form.get('employeeId')?.disable(); // Lock employee ID on edit
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
  }

  saveContract() {
    if (this.form.invalid) return;

    this.isSaving.set(true);
    const payload = this.form.getRawValue();

    if (this.isEditing() && this.currentContractId) {
      this.api.updateContract(this.currentContractId, payload).subscribe({
        next: () => {
          this.isSaving.set(false);
          this.closeModal();
          this.loadData();
        },
        error: (err: any) => {
          this.isSaving.set(false);
          alert(err.error?.message || 'Error updating contract');
        }
      });
    } else {
      this.api.createContract(payload).subscribe({
        next: () => {
          this.isSaving.set(false);
          this.closeModal();
          this.loadData();
        },
        error: (err: any) => {
          this.isSaving.set(false);
          alert(err.error?.message || 'Error creating contract');
        }
      });
    }
  }

  deleteContract(id: number) {
    if (confirm('Are you sure you want to completely delete this contract?')) {
      this.api.deleteContract(id).subscribe({
        next: () => this.loadData(),
        error: (err: any) => alert(err.error?.message || 'Error deleting contract')
      });
    }
  }
}
