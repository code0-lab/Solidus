import { Component, Input, inject, signal, OnChanges, SimpleChanges, effect } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CommentService } from '../../../services/comment.service';
import { AuthService } from '../../../services/auth.service';
import { ToastService } from '../../../services/toast.service';
import { Comment } from '../../../models/comment.model';

@Component({
  selector: 'app-product-comments',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './product-comments.html',
  styleUrl: './product-comments.css'
})
export class ProductCommentsComponent implements OnChanges {
  @Input({ required: true }) productId!: number;

  commentService = inject(CommentService);
  authService = inject(AuthService);
  toastService = inject(ToastService);
  newCommentText = signal('');
  
  editingCommentId = signal<number | null>(null);
  editCommentText = signal('');

  constructor() {
    effect(() => {
      // Trigger refetch when user status changes (login/logout)
      this.authService.currentUser();
      if (this.productId) {
        this.commentService.fetchComments(this.productId);
      }
    });
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['productId'] && this.productId) {
      this.commentService.fetchComments(this.productId);
    }
  }

  postComment() {
    const text = this.newCommentText();
    if (!text.trim()) return;

    if (!this.authService.currentUser()) {
      this.toastService.info('You must be logged in to post a comment.');
      this.authService.toggleLogin();
      return;
    }

    this.commentService.createComment({ productId: this.productId, text }).subscribe({
      next: () => {
        this.newCommentText.set('');
        this.toastService.success('Your comment has been added!');
      },
      error: (err) => {
        console.error(err);
        this.toastService.error('Error adding comment.');
      }
    });
  }

  startEdit(comment: Comment) {
    this.editingCommentId.set(comment.id);
    this.editCommentText.set(comment.text);
  }

  cancelEdit() {
    this.editingCommentId.set(null);
    this.editCommentText.set('');
  }

  saveEdit(comment: Comment) {
    const text = this.editCommentText().trim();
    if (!text) return;

    if (text === comment.text) {
      this.cancelEdit();
      return;
    }

    this.commentService.updateComment(comment.id, text, this.productId).subscribe(() => {
      this.toastService.success('Comment updated successfully');
      this.cancelEdit();
    });
  }
}
