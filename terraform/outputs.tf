output "s3_bucket_name" {
  value = aws_s3_bucket.bank_statement_converter.bucket
}

output "sqs_queue_url" {
  value = aws_sqs_queue.conversion_jobs.id
}

output "sqs_queue_arn" {
  value = aws_sqs_queue.conversion_jobs.arn
}

output "textract_policy_arn" {
  value = aws_iam_policy.textract_policy.arn
}
