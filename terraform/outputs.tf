output "s3_bucket_name" {
  value = aws_s3_bucket.bank_statement_converter.bucket
}

output "bank_statement_sqs_queue_url" {
  value = aws_sqs_queue.conversion_jobs.id
}

output "bank_statement_sqs_queue_arn" {
  value = aws_sqs_queue.conversion_jobs.arn
}

output "public_bank_statement_sqs_queue_url" {
  value = aws_sqs_queue.public_conversion_jobs.id
}

output "public_bank_statement_sqs_queue_arn" {
  value = aws_sqs_queue.public_conversion_jobs.arn
}

output "delivery_receipt_sqs_queue_url" {
  value = aws_sqs_queue.delivery_receipt_conversion_jobs.id
}

output "delivery_receipt_sqs_queue_arn" {
  value = aws_sqs_queue.delivery_receipt_conversion_jobs.arn
}

output "textract_policy_arn" {
  value = aws_iam_policy.textract_policy.arn
}
