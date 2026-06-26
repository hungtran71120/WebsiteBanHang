export interface Banner {
  id: string
  imageUrl: string
  title: string
  subtitle: string | null
  linkUrl: string
  displayOrder: number
  isActive: boolean
}

export interface CreateBannerRequest {
  title: string
  subtitle?: string | null
  linkUrl: string
  displayOrder: number
  isActive: boolean
}

export interface UpdateBannerRequest {
  title: string
  subtitle?: string | null
  linkUrl: string
  displayOrder: number
  isActive: boolean
}
