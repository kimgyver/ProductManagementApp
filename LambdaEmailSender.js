import { SendEmailCommand, SESClient } from "@aws-sdk/client-ses";
import { SQSClient, SendMessageCommand } from "@aws-sdk/client-sqs";

const sesClient = new SESClient({ region: "ap-southeast-2" });
const sqsClient = new SQSClient({ region: "ap-southeast-2" });

const FAILURE_QUEUE_URL =
  "https://sqs.ap-southeast-2.amazonaws.com/840297437975/EmailFailureQueue";

export const handler = async (event, context) => {
  try {
    for (const record of event.Records) {
      const messageBody = JSON.parse(record.body);
      const { Email, Subject, Body } = messageBody;

      try {
        const sendRequest = new SendEmailCommand({
          Source: "jasonkim4747@gmail.com",
          Destination: { ToAddresses: [Email] },
          Message: {
            Subject: { Data: Subject },
            Body: { Text: { Data: Body } }
          }
        });

        await sesClient.send(sendRequest);
        console.log(`✅ Email sent to ${Email}`);
      } catch (error) {
        console.error(`❌ Error sending email to ${Email}:`, error);

        if (error.name === "MessageRejected") {
          // Send failure message to SQS
          await sqsClient.send(
            new SendMessageCommand({
              QueueUrl: FAILURE_QUEUE_URL,
              MessageBody: JSON.stringify({ Email, Status: "Failed" }) // Send UserId
            })
          );
          console.log(
            `✅ Message is sent to EmailFailureQueue - email: ${Email}`
          );

          // console.log("Skipping retries for unverified email:", Email);
          // // Manually remove from SQS to avoid infinite retries
          // await deleteSQSMessage(record.receiptHandle);
        }

        // Ensure message is retried and moved to DLQ on failure
        throw error;
      }
    }
  } catch (error) {
    console.error("❌ Lambda failed:", error);
  }
};

async function deleteSQSMessage(receiptHandle) {
  const { SQSClient, DeleteMessageCommand } = await import(
    "@aws-sdk/client-sqs"
  );
  const sqsClient = new SQSClient({ region: "ap-southeast-2" });

  const deleteCommand = new DeleteMessageCommand({
    QueueUrl: "https://sqs.ap-southeast-2.amazonaws.com/840297437975/myNewSQS",
    ReceiptHandle: receiptHandle
  });

  await sqsClient.send(deleteCommand);
  console.log("🚀 Message deleted from SQS");
}
