variable "aws_region" {
  description = "AWS region to deploy resources"
  default     = "ap-southeast-2"
}

variable "project_name" {
  description = "Project prefix for naming"
  default     = "fino"
}

variable "iam_user_name" {
  description = "IAM user that runs the worker locally"
  type        = string
  default     = "admin"    
}
