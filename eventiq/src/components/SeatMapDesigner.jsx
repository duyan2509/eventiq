import React, { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Spin } from 'antd';
import { eventAPI } from '../services/api';
import { useMessage } from '../hooks/useMessage';
import { SeatsioDesigner, SeatsioSeatingChart } from '@seatsio/seatsio-react';
import { useAuth } from '../contexts/AuthContext';

const SeatMapDesigner = () => {
    const { eventId, chartId } = useParams();
    const { error, contextHolder } = useMessage();
    const { user } = useAuth();
    const [loading, setLoading] = useState(true);
    const [chart, setChart] = useState(null);
    const [seatMapData, setSeatMapData] = useState(null);
    
    // Determine editable:
    // - Org role or Event.Update permission from token/user
    // - Or server returned seatMapData.isReadOnly === false
    const canEditByRole = (Array.isArray(user?.roles) && user.roles.includes('Org'))
        || (typeof user?.roles === 'string' && user.roles === 'Org')
        || (Array.isArray(user?.permission) && user.permission.includes('Event.Update'));
    const canEditByServer = seatMapData?.isReadOnly === false;
    const isOrg = canEditByRole || canEditByServer;

    useEffect(() => {
        const loadChart = async () => {
            try {
                console.log('Loading chart with eventId:', eventId, 'chartId:', chartId);
                const chartData = await eventAPI.getEventChart(eventId, chartId);
                console.log('Chart data received:', chartData);
                setChart(chartData);

                // Load seat map data from DB (for viewing existing seats with status)
                try {
                    const seatMap = await eventAPI.getSeatMapForView(eventId, chartId);
                    setSeatMapData(seatMap);
                } catch (err) {
                    console.log('No existing seat data from DB');
                }
            } catch (err) {
                console.error('Error loading chart:', err);
                error('Failed to load chart data');
            } finally {
                setLoading(false);
            }
        };

        loadChart();
    }, [eventId, chartId]);


    if (loading) {
        return (
            <div className="flex justify-center items-center min-h-screen">
                {contextHolder}
                <Spin size="large" tip="Loading seat map designer..." />
            </div>
        );
    }

    // Nếu không có chartKey, không thể hiển thị
    if (!chart?.key) {
        return (
            <div className="flex justify-center items-center min-h-screen">
                {contextHolder}
                <div className="text-red-500">No seat map configuration available</div>
            </div>
        );
    }

    const publicKey = import.meta.env.VITE_SEAT_IO_PUBLIC_KEY;
    // Secret key is required for Seats.io Designer (per official SDK)
    // NOTE: exposing secret key in frontend is not recommended for production
    const secretKey = import.meta.env.VITE_SEAT_IO_SECRET_KEY || publicKey;
    if (!publicKey || !secretKey) {
        return (
            <div className="flex justify-center items-center min-h-screen">
                {contextHolder}
                <div className="text-red-500">Seat.io keys not configured</div>
            </div>
        );
    }

    // Prepare booked objects for Seats.io viewer from DB data
    const bookedObjects = seatMapData?.seats
        ?.filter(seat => seat.status === 'paid')
        ?.map(seat => seat.seatKey) || [];

    // Prepare selected objects (hold status) - will come from Redis in future
    const selectedObjects = seatMapData?.seats
        ?.filter(seat => seat.status === 'hold')
        ?.map(seat => seat.seatKey) || [];

    console.log('Rendering with publicKey:', publicKey, 'chartKey:', chart?.key, 'isOrg:', isOrg);
    
    return (
        <div style={{ height: '100vh', width: '100%', position: 'relative' }}>
            {contextHolder}
            
            {isOrg ? (
                <SeatsioDesigner
                    secretKey={secretKey}
                    chartKey={chart.key}
                    priceFormatter={price => '₫' + price}
                    region='OC'
                    language="en"
                    onChartCreated={async (chartKey) => {
                        console.log('Chart created with key:', chartKey);
                        // If current chart.key is GUID, update it with the new chartKey from Seats.io
                        const isGuidFormat = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(chart.key);
                        if (isGuidFormat && chartKey && chartKey !== chart.key) {
                            console.log('Updating chart key from', chart.key, 'to', chartKey);
                            try {
                                // Update chart key in database
                                await eventAPI.updateEventChart(eventId, chartId, {
                                    name: chart.name,
                                    chartKey: chartKey
                                });
                                // Update local state
                                setChart({ ...chart, key: chartKey });
                                console.log('Chart key updated successfully');
                            } catch (err) {
                                console.error('Failed to update chart key:', err);
                                error('Failed to update chart key. Please try saving again.');
                            }
                        }
                    }}
                    

                />
            ) : (
                // Viewer mode for regular users - show seats with status from DB
                <SeatsioSeatingChart
                    workspaceKey={publicKey}
                    chartKey={chart.key}
                    bookedObjects={bookedObjects}
                    selectedObjects={selectedObjects}
                    priceFormatter={price => '₫' + price}
                    region='OC'
                    language="en"
                    onObjectSelected={(selectedObject) => {
                        console.log('Seat selected:', selectedObject);
                    }}
                />
            )}
        </div>
    );
};

export default SeatMapDesigner;