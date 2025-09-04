resource "aws_sqs_queue" "conversion_jobs" {
  name = "${var.project_name}-conversion-jobs"

  # Optional tuning
  visibility_timeout_seconds = 300   # matches worker job time
  message_retention_seconds  = 86400 # keep messages for 1 day
  delay_seconds              = 0
  max_message_size           = 262144 # 256 KB

  tags = {
    Project = var.project_name
  }
}

resource "aws_sqs_queue" "public_conversion_jobs" {
  name = "${var.project_name}-public-conversion-jobs"

  # Optional tuning
  visibility_timeout_seconds = 300   # matches worker job time
  message_retention_seconds  = 86400 # keep messages for 1 day
  delay_seconds              = 0
  max_message_size           = 262144 # 256 KB

  tags = {
    Project = var.project_name
  }
}
