﻿using System;
using System.Messaging;

namespace MessageBusPatterns.MessageBus.InventoryProcessor
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create an instance of MessageQueue. Set its formatter.
            MessageQueue mq = new MessageQueue(@".\private$\mbp.inventory");
            mq.Formatter = new XmlMessageFormatter(new[] { typeof(String) });

            // Add an event handler for the PeekCompleted event.
            mq.PeekCompleted += OnPeekCompleted;

            // Begin the asynchronous peek operation.
            mq.BeginPeek();

            Console.WriteLine("Inventory processor listening on queue");

            Console.ReadLine();

            Console.WriteLine("Closing inventory processor...");

            mq.Close();
            mq.Dispose();

            Console.WriteLine("Inventory processor closed. Press any key to exit.");
            Console.ReadLine();
        }

        private static void OnPeekCompleted(Object source, PeekCompletedEventArgs asyncResult)
        {
            // Connect to the queue.
            MessageQueue mq = (MessageQueue)source;

            try // catch errors that occur when we close a message queue
            {
                mq.EndPeek(asyncResult.AsyncResult);
            }
            catch (Exception)
            {
                return;
            }

            // create transaction
            using (var txn = new MessageQueueTransaction())
            {
                try
                {
                    // retrieve message and process
                    txn.Begin();
                    // End the asynchronous peek operation.
                    var message = mq.Receive(txn);

                    // Display message information on the screen.
                    if (message != null)
                    {
                        Console.WriteLine("Message Processed: {0}: {1}", message.Label, (string)message.Body);
                    }

                    // message will be removed on txn.Commit.
                    txn.Commit();
                }
                catch (Exception ex)
                {
                    // on error don't remove message from queue
                    Console.WriteLine(ex.ToString());
                    txn.Abort();
                }
            }

            // Restart the asynchronous peek operation.
            mq.BeginPeek();
        }
    }
}