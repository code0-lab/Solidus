import { Component, ElementRef, ViewChild, AfterViewInit, HostListener, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './not-found.component.html',
  styleUrl: './not-found.component.css'
})
export class NotFoundComponent implements AfterViewInit, OnDestroy {
  @ViewChild('rainCanvas') rainCanvas?: ElementRef<HTMLCanvasElement>;

  private ctx?: CanvasRenderingContext2D;
  private drops: { x: number; y: number; length: number; speed: number; opacity: number }[] = [];
  private animationFrameId: number | null = null;

  ngAfterViewInit(): void {
    const canvas = this.rainCanvas?.nativeElement;
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    this.ctx = ctx;
    this.resizeCanvas();
    this.initDrops(100);
    this.animate();
  }

  ngOnDestroy(): void {
    if (this.animationFrameId !== null) {
      cancelAnimationFrame(this.animationFrameId);
    }
  }

  @HostListener('window:resize')
  onResize(): void {
    this.resizeCanvas();
  }

  private resizeCanvas(): void {
    const canvas = this.rainCanvas?.nativeElement;
    if (!canvas) return;

    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
  }

  private initDrops(count: number): void {
    const canvas = this.rainCanvas?.nativeElement;
    if (!canvas || !this.ctx) return;

    this.drops = [];
    for (let i = 0; i < count; i++) {
      this.drops.push({
        x: Math.random() * canvas.width,
        y: Math.random() * canvas.height,
        length: Math.random() * 20 + 20, // Longer drops
        speed: Math.random() * 10 + 15,   // Faster
        opacity: Math.random() * 0.5 + 0.3 // More visible
      });
    }
  }

  private animate(): void {
    const canvas = this.rainCanvas?.nativeElement;
    if (!canvas || !this.ctx) return;

    this.ctx.clearRect(0, 0, canvas.width, canvas.height);

    for (const drop of this.drops) {
      drop.y += drop.speed;
      if (drop.y > canvas.height) {
        drop.y = -drop.length;
        drop.x = Math.random() * canvas.width;
      }

      this.ctx.beginPath();
      this.ctx.moveTo(drop.x, drop.y);
      this.ctx.lineTo(drop.x, drop.y + drop.length);
      // Realistic rain color (light blue-ish white)
      this.ctx.strokeStyle = `rgba(174, 216, 255, ${drop.opacity})`;
      this.ctx.lineWidth = 2;
      this.ctx.stroke();
    }

    this.animationFrameId = requestAnimationFrame(() => this.animate());
  }

}
