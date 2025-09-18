import React, { createContext, useContext, useEffect, useState, ReactNode } from 'react';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

interface SignalRContextType {
  connection: HubConnection | null;
  isConnected: boolean;
  joinFlightGroup: (flightNumber: string) => Promise<void>;
  leaveFlightGroup: (flightNumber: string) => Promise<void>;
}

const SignalRContext = createContext<SignalRContextType | undefined>(undefined);

export const useSignalR = () => {
  const context = useContext(SignalRContext);
  if (context === undefined) {
    throw new Error('useSignalR must be used within a SignalRProvider');
  }
  return context;
};

interface SignalRProviderProps {
  children: ReactNode;
}

export const SignalRProvider: React.FC<SignalRProviderProps> = ({ children }) => {
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const newConnection = new HubConnectionBuilder()
      .withUrl('http://localhost:5000/flightUpdatesHub')
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    setConnection(newConnection);

    return () => {
      if (newConnection) {
        newConnection.stop();
      }
    };
  }, []);

  useEffect(() => {
    if (connection) {
      connection
        .start()
        .then(() => {
          console.log('SignalR Connected');
          setIsConnected(true);

          connection.on('Connected', (connectionId) => {
            console.log('Connected with ID:', connectionId);
          });

          connection.on('FlightStatusChanged', (update) => {
            console.log('Flight status update:', update);
            // Handle flight status updates
          });

          connection.on('GateChanged', (update) => {
            console.log('Gate change update:', update);
            // Handle gate change updates
          });

          connection.on('FlightDelayed', (update) => {
            console.log('Flight delay update:', update);
            // Handle flight delay updates
          });
        })
        .catch((error) => {
          console.error('SignalR Connection Error:', error);
          setIsConnected(false);
        });

      connection.onclose(() => {
        setIsConnected(false);
        console.log('SignalR Disconnected');
      });

      connection.onreconnected(() => {
        setIsConnected(true);
        console.log('SignalR Reconnected');
      });
    }
  }, [connection]);

  const joinFlightGroup = async (flightNumber: string) => {
    if (connection && isConnected) {
      try {
        await connection.invoke('JoinFlightGroup', flightNumber);
        console.log(`Joined flight group: ${flightNumber}`);
      } catch (error) {
        console.error('Error joining flight group:', error);
      }
    }
  };

  const leaveFlightGroup = async (flightNumber: string) => {
    if (connection && isConnected) {
      try {
        await connection.invoke('LeaveFlightGroup', flightNumber);
        console.log(`Left flight group: ${flightNumber}`);
      } catch (error) {
        console.error('Error leaving flight group:', error);
      }
    }
  };

  const value = {
    connection,
    isConnected,
    joinFlightGroup,
    leaveFlightGroup
  };

  return (
    <SignalRContext.Provider value={value}>
      {children}
    </SignalRContext.Provider>
  );
};