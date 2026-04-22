export interface RenovateMrDto {
  repo: string
  iid: number
  title: string
  webUrl: string
  createdAt: string
  author: string
  sourceBranch: string
  targetBranch: string
  pipelineStatus: string | null
}
