import { Component, inject, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { BannerService } from '../../services/banner.service';
import { Banner } from '../../models/banner.model';

@Component({
  selector: 'app-slider',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './slider.component.html',
  styleUrl: './slider.component.css'
})
export class SliderComponent implements OnInit {
  private bannerService = inject(BannerService);
  private sanitizer = inject(DomSanitizer);

  banner = computed<Banner | null>(() => this.bannerService.banner());

  safeHtml = computed<SafeHtml>(() => {
    const content = this.banner()?.htmlContent;
    if (!content) return '';

    const html = `
      <!DOCTYPE html>
      <html lang="en">
      <head>
        <meta charset="UTF-8">
        <meta name="viewport" content="width=device-width, initial-scale=1.0">
        <style>
          body {
            margin: 0;
            padding: 0;
            width: 100vw;
            height: 100vh;
            overflow: hidden;
            display: flex;
            justify-content: center;
            align-items: center;
          }
          /* Ensure all children are responsive */
          * {
            max-width: 100%;
            box-sizing: border-box;
          }
        </style>
      </head>
      <body>
        ${content}
      </body>
      </html>
    `;
    return this.sanitizer.bypassSecurityTrustHtml(html);
  });

  ngOnInit() {
    this.bannerService.loadActiveBanner();
  }
}
