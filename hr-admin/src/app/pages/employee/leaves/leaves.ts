import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { HrApi, Leave } from '../../../services/hr-api';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-employee-leaves',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './leaves.html',
  styleUrls: ['./leaves.scss']
})
export class Leaves implements OnInit {
  leaves = signal<Leave[]>([]);
  isLoading = signal(true);
  
  // Create Modal State
  showModal = signal(false);
  isSaving = signal(false);
  form: FormGroup;

  // The logged-in user details for the sidebar
  user: any = null;

  constructor(
    public auth: AuthService, 
    private api: HrApi, 
    private fb: FormBuilder
  ) {
    this.user = this.auth.currentUser;
    this.form = this.fb.group({
      type: ['Vacation', Validators.required],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      reason: ['']
    });
  }

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.isLoading.set(true);
    this.api.getMyLeaves().subscribe({
      next: (data) => {
        this.leaves.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error(err);
        this.isLoading.set(false);
      }
    });
  }

  openCreateModal() {
    this.form.reset({ type: 'Vacation' });
    this.showModal.set(true);
  }

  closeModal() {
    this.showModal.set(false);
  }

  submitLeave() {
    if (this.form.invalid) return;

    // Validate dates
    const start = new Date(this.form.value.startDate);
    const end = new Date(this.form.value.endDate);
    
    if (end < start) {
      alert("End Date cannot be before Start Date.");
      return;
    }

    this.isSaving.set(true);
    this.api.createLeave(this.form.value).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.closeModal();
        this.loadData();
      },
      error: (err: any) => {
        this.isSaving.set(false);
        alert(err.error?.message || 'Error submitting leave request');
      }
    });
  }

  deleteLeave(id: number) {
    if (confirm('Cancel this pending leave request entirely?')) {
      this.api.deleteLeave(id).subscribe({
        next: () => this.loadData(),
        error: (err: any) => alert(err.error?.message || 'Error cancelling request')
      });
    }
  }

  logout() {
    this.auth.logout();
  }
}
