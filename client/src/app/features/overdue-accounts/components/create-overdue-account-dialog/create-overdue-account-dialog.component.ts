import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { OverdueAccountService } from '../../services/overdue-account.service';

@Component({
  selector: 'app-create-overdue-account-dialog',
  templateUrl: './create-overdue-account-dialog.component.html',
  styleUrls: ['./create-overdue-account-dialog.component.scss'],
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule
  ]
})
export class CreateOverdueAccountDialogComponent implements OnInit {
  form: FormGroup;

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<CreateOverdueAccountDialogComponent>,
    private overdueAccountService: OverdueAccountService
  ) {
    this.form = this.fb.group({
      description: ['', [Validators.required, Validators.maxLength(200)]],
      currentDebtValue: ['', [Validators.required, Validators.min(0)]]
    });
  }

  ngOnInit(): void { }

  onSave(): void {
    if (this.form.valid) {
      const formValue = this.form.value;

      this.overdueAccountService.createOverdueAccount({
        description: formValue.description,
        currentDebtValue: formValue.currentDebtValue
      }).subscribe({
        next: () => {
          this.dialogRef.close(true);
        },
        error: (error) => {
          console.error('Erro ao criar conta atrasada:', error);
        }
      });
    }
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
