import { Component, Input, inject, signal, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CommentService } from '../../../services/comment.service';
import { AuthService } from '../../../services/auth.service';

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
  newCommentText = signal('');

  ngOnChanges(changes: SimpleChanges) {
    if (changes['productId'] && this.productId) {
      this.commentService.fetchComments(this.productId);
    }
  }

  postComment() {
    const text = this.newCommentText();
    if (!text.trim()) return;

    if (!this.authService.currentUser()) {
      alert('You must be logged in to post a comment.');
      this.authService.toggleLogin();
      return;
    }

    this.commentService.createComment({ productId: this.productId, text }).subscribe({
      next: () => {
        this.newCommentText.set('');
        alert('Your comment has been added!');
      },
      error: (err) => {
        console.error(err);
        alert('Error adding comment.');
      }
    });
  }
}
