import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Typography,
  Spin,
  Empty,
  Button,
  Space,
  Alert,
  FloatButton,
  Modal,
  Descriptions,
  Tag,
} from 'antd';
import { DollarOutlined, ArrowLeftOutlined, ShoppingCartOutlined, CloseOutlined } from '@ant-design/icons';
import { SeatsioSeatingChart } from '@seatsio/seatsio-react';
import { customerAPI, checkoutAPI } from '../services/api';

const { Title, Text } = Typography;

const CustomerSeatMap = () => {
  const { eventId, eventItemId } = useParams();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(true);
  const [seatMap, setSeatMap] = useState(null);
  const [error, setError] = useState(null);
  const [chartRendering, setChartRendering] = useState(false);
  const [chartInitialized, setChartInitialized] = useState(false);
  const [chartInstance, setChartInstance] = useState(null);
  const [selectionUpdateTrigger, setSelectionUpdateTrigger] = useState(0);
  const [clickedSeat, setClickedSeat] = useState(null);
  const [seatModalVisible, setSeatModalVisible] = useState(false);
  const [cartVisible, setCartVisible] = useState(false);


  useEffect(() => {
    if (eventId && eventItemId) {
      fetchSeatMap();
    }
  }, [eventId, eventItemId]);

  // Timeout for chart rendering - auto-hide loading after 30 seconds
  useEffect(() => {
    if (chartRendering) {
      const timeout = setTimeout(() => {
        console.warn('Chart rendering timeout - hiding loading spinner after 30 seconds');
        setChartRendering(false);
      }, 30000); // 30 seconds timeout

      return () => clearTimeout(timeout);
    }
  }, [chartRendering]);

  // Update chart instance when selection changes to get latest selectedObjects
  useEffect(() => {
    if (chartInstance && selectionUpdateTrigger > 0) {
      // Small delay to ensure chart has updated its selectedObjects
      const timeout = setTimeout(() => {
        if (chartInstance) {
          setChartInstance({ ...chartInstance });
        }
      }, 100);
      return () => clearTimeout(timeout);
    }
  }, [selectionUpdateTrigger]);

  const fetchSeatMap = async () => {
    try {
      setLoading(true);
      setError(null);
      setChartInitialized(false); // Reset when fetching new seat map
      const data = await customerAPI.getEventItemSeatMap(eventId, eventItemId);
      console.log('Seat map data received:', data);
      console.log('ChartKey:', data.chartKey);
      console.log('Public Key:', import.meta.env.VITE_SEAT_IO_PUBLIC_KEY);

      setSeatMap(data);


    } catch (error) {
      console.error('Error fetching seat map:', error);
      setError(error.response?.data?.message || error.message || 'Failed to load seat map');
    } finally {
      setLoading(false);
    }
  };



  // Helper function to map seat keys from chart.selectedObjects to seat data with prices
  const getSelectedSeatsData = () => {
    if (!chartInstance || !chartInstance.selectedObjects || chartInstance.selectedObjects.length === 0) {
      return [];
    }
    return chartInstance.selectedObjects.map(seatKey => {
      const seat = seatMap?.seats.find((s) => s.seatKey === seatKey);
      
      let price = 0;
      let seatCategory = null;
      
      if (!seatCategory) {
        seatCategory = seat?.categoryKey || seat?.category;
        console.log('Seat category:', seatCategory);
      }      
      console.log('Chart instance:', chartInstance);
      price = chartInstance?.config?.pricing?.prices.
        find(p => p.category === seatCategory)?.price;

      console.log('Price:', price);

      console.log('Result:',{
        seatKey: seatKey,
        label: seat?.label || seatKey,
        price: Number(price),
        categoryKey: seatCategory,
        status: seat?.status || 'available',
        ...seat,
      });
      return {
        ...seat,
        label: seat?.label || seatKey,
        price: Number(price),
        categoryKey: seatCategory,
        status: seat?.status || 'available',
      };
    });
  };

  const handleObjectClick = (clickedObject) => {
    console.log('Seat clicked:', clickedObject);
    // Handle both single object and array cases (in case multiple objects are clicked)
    const clickedObj = Array.isArray(clickedObject)
      ? clickedObject[0]
      : clickedObject;

    // Seats.io uses 'id' instead of 'objectId'
    const seatId = clickedObj?.id || clickedObj?.objectId;

    if (!clickedObj || !seatId) {
      console.warn('Invalid clicked object:', clickedObject);
      return;
    }

    // Find seat details from seatMap
    const seat = seatMap?.seats.find((s) => s.seatKey === seatId);

    // Get price from object or from seat data
    const seatPrice = clickedObj.price || clickedObj.pricing?.price || seat?.price || 0;

    const seatInfo = {
      seatKey: seatId,
      label: clickedObj.label || clickedObj.labels?.own || seatId,
      price: seatPrice,
      categoryKey: clickedObj.categoryKey || clickedObj.category?.key,
      status: seat?.status || 'available',
      ...seat,
    };
    setClickedSeat(seatInfo);
    setSeatModalVisible(true);
  };

  const handleProceedToCheckout = async () => {
    const selectedSeatsData = getSelectedSeatsData();
    if (selectedSeatsData.length === 0) {
      console.warn('No seats selected, cannot proceed to checkout');
      return;
    }
    
    console.log('Proceeding to checkout with seats:', selectedSeatsData);
    
    try {
      // Call backend to create checkout session
      const seatIds = selectedSeatsData.map(s => s.seatKey);
      const checkout = await checkoutAPI.createCheckout(eventItemId, seatIds);
      
      console.log('Checkout created:', checkout);
      
      // Save checkout info to localStorage for payment page
      localStorage.setItem('checkoutId', checkout.id);
      localStorage.setItem('selectedSeats', JSON.stringify(selectedSeatsData));
      localStorage.setItem('eventId', eventId);
      localStorage.setItem('eventItemId', eventItemId);
      
      // Navigate to payment page
      navigate('/payment');
    } catch (error) {
      console.error('Error creating checkout:', error);
      const errorMessage = error.response?.data?.message || error.message || 'Failed to create checkout. Some seats may already be taken.';
      setError(errorMessage);
    }
  };

  const getTotalPrice = () => {
    const selectedSeatsData = getSelectedSeatsData();
    return selectedSeatsData.reduce((sum, seat) => sum + (seat.price || 0), 0);
  };

  const getSelectedSeatsCount = () => {
    return chartInstance?.selectedObjects?.length || 0;
  };

  const formatPrice = (price) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
    }).format(price);
  };

  const publicKey = import.meta.env.VITE_SEAT_IO_PUBLIC_KEY;

  if (loading) {
    return (
      <div style={{
        height: '100vh',
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center'
      }}>
        <Spin size="large" tip="Loading seat map..." />
      </div>
    );
  }

  if (error) {
    return (
      <div style={{
        height: '100vh',
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        padding: '50px'
      }}>
        <Alert
          message="Error"
          description={error}
          type="error"
          showIcon
          action={
            <Button size="small" onClick={() => navigate(-1)}>
              Go Back
            </Button>
          }
        />
      </div>
    );
  }

  if (!seatMap) {
    return (
      <div style={{
        height: '100vh',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'center',
        alignItems: 'center'
      }}>
        <Empty description="Seat map not found" />
        <Button type="primary" onClick={() => navigate(-1)} style={{ marginTop: 16 }}>
          Go Back
        </Button>
      </div>
    );
  }

  // Check for required configuration
  if (!publicKey) {
    return (
      <div style={{
        height: '100vh',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'center',
        alignItems: 'center',
        padding: '50px'
      }}>
        <Alert
          message="Configuration Error"
          description="Seats.io public key is not configured. Please check VITE_SEAT_IO_PUBLIC_KEY in .env file."
          type="error"
          showIcon
        />
        <Button type="primary" onClick={() => navigate(-1)} style={{ marginTop: 16 }}>
          Go Back
        </Button>
      </div>
    );
  }

  // Check if we have either eventKey or chartKey
  const hasKey = (seatMap.eventKey && seatMap.eventKey.trim() !== '') ||
    (seatMap.chartKey && seatMap.chartKey.trim() !== '');

  if (!hasKey) {
    return (
      <div style={{
        height: '100vh',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'center',
        alignItems: 'center',
        padding: '50px'
      }}>
        <Alert
          message="Seat Map Not Available"
          description="This session does not have a seat map configured yet."
          type="warning"
          showIcon
        />
        <Button type="primary" onClick={() => navigate(-1)} style={{ marginTop: 16 }}>
          Go Back
        </Button>
      </div>
    );
  }


  // Use EventKey if available (preferred), otherwise fallback to ChartKey
  const eventKey = seatMap.eventKey || seatMap.chartKey;

  // If we have eventKey from Seats.io, use it directly (no need to check GUID)
  // Only check GUID format if we're using chartKey as fallback
  const usingEventKey = !!seatMap.eventKey;
  const isGuidFormat = !usingEventKey && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(eventKey);

  let venueDefinition = null;
  if (seatMap.venueDefinition) {
    try {
      venueDefinition = typeof seatMap.venueDefinition === 'string'
        ? JSON.parse(seatMap.venueDefinition)
        : seatMap.venueDefinition;
    } catch (e) {
      console.warn('Failed to parse venueDefinition:', e);
    }
  }

  // If using chartKey (not eventKey) and it's GUID format but no venueDefinition, cannot render
  if (!usingEventKey && isGuidFormat && !venueDefinition) {
    return (
      <div style={{
        height: '100vh',
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'center',
        alignItems: 'center',
        padding: '50px'
      }}>
        <Alert
          message="Seat Map Not Configured"
          description={
            <div>
              <p>The seat map for this session has not been properly configured yet.</p>
              <p>The chart key appears to be a temporary identifier instead of a Seats.io chart key.</p>
              <p style={{ marginTop: 8 }}>Please contact the event organizer to complete the seat map setup.</p>
            </div>
          }
          type="warning"
          showIcon
        />
        <Button type="primary" onClick={() => navigate(-1)} style={{ marginTop: 16 }}>
          Go Back
        </Button>
      </div>
    );
  }

  // Build pricing configuration from ticketClasses
  // Pricing is set at category level - each category (ticketClass.Name) gets its price
  const pricingConfig = seatMap.ticketClasses && seatMap.ticketClasses.length > 0
    ? {
      priceFormatter: (price) => '₫' + price,
      prices: seatMap.ticketClasses.map(tc => ({
        category: tc.name || tc.Name, // Category key is ticketClass.Name
        price: tc.price || tc.Price || 0
      }))
    }
    : null;

  console.log('Rendering Seats.io chart with:', {
    workspaceKey: publicKey,
    eventKey: seatMap.eventKey,
    chartKey: seatMap.chartKey,
    usingEventKey: usingEventKey,
    finalEventKey: eventKey,
    isGuid: isGuidFormat,
    hasVenueDefinition: !!venueDefinition,
    totalSeats: seatMap.seats.length,
    pricingConfig: pricingConfig,
  });

  return (
    <div style={{
      height: '100vh',
      width: '100vw',
      position: 'fixed',
      top: 0,
      left: 0,
      backgroundColor: '#f9fafb',
      display: 'flex',
      flexDirection: 'column'
    }}>
      {/* Header */}
      <div style={{
        padding: '16px 24px',
        backgroundColor: '#fff',
        borderBottom: '1px solid #d9d9d9',
        zIndex: 1000,
        flexShrink: 0
      }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 12 }}>
          <div style={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 12 }}>
            <Button
              icon={<ArrowLeftOutlined />}
              onClick={() => navigate(-1)}
            >
              Back
            </Button>
            <Title level={4} style={{ margin: 0 }}>
              {seatMap.eventItemName}
            </Title>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 12 }}>
            {getSelectedSeatsCount() > 0 && (
              <Text strong style={{ color: '#f5222d', fontSize: 16 }}>
                <DollarOutlined /> {formatPrice(getTotalPrice())}
              </Text>
            )}
          </div>
        </div>
      </div>

      {/* Seats.io Chart Container - Full Screen */}
      <div style={{
        flex: 1,
        width: '100%',
        position: 'relative',
        overflow: 'hidden',
        minHeight: 0
      }}>
        {chartRendering && !chartInitialized && (
          <div style={{
            position: 'absolute',
            top: '50%',
            left: '50%',
            transform: 'translate(-50%, -50%)',
            zIndex: 10
          }}>
            <Spin size="large" tip="Rendering seat map..." />
          </div>
        )}
        {eventKey && (
          <SeatsioSeatingChart
            key={`${eventKey}-${usingEventKey}`} // Force re-render when key changes
            workspaceKey={publicKey}
            {...(venueDefinition && !usingEventKey
              ? { event: venueDefinition }
              : { event: eventKey })}
            {...(seatMap.maxPerUser > 0 || seatMap.MaxPerUser > 0
              ? {
                maxSelectedObjects: (seatMap.maxPerUser || seatMap.MaxPerUser || 0) // Total max seats per user for this event item
              }
              : {})}
            {...(pricingConfig ? { pricing: pricingConfig } : {})}
            region="OC"
            language="en"
            onRenderStarted={(chart) => {
              console.log('Seats.io chart render started', chart);
              console.log("eventiq", chart.selectedObjects);
              setChartInstance(chart); // Save chart instance to access selectedObjects

              // Only show spinner if chart hasn't been initialized yet
              if (!chartInitialized) {
                setChartRendering(true);
              }
              setError(null); // Clear any previous errors
            }}
            onObjectSelected={(selectedObject) => {
              // Trigger re-render to update UI with new selectedObjects
              setSelectionUpdateTrigger(prev => prev + 1);
            }}
            onObjectDeselected={(deselectedObject) => {
              // Trigger re-render to update UI with new selectedObjects
              setSelectionUpdateTrigger(prev => prev + 1);
            }}
            onObjectClick={handleObjectClick}
            onChartRendered={(chart) => {
              console.log('Seats.io chart rendered successfully', chart);
              console.log("eventiq selectedObjects after render:", chart.selectedObjects);
              setChartInstance(chart); // Save chart instance
              setChartRendering(false);
              setChartInitialized(true); // Mark chart as initialized
              setError(null); // Clear any errors on success
            }}
            onChartRenderingFailed={(error) => {
              console.error('Seats.io chart rendering failed:', error);
              console.error('Error details:', {
                error,
                message: error?.message,
                config: error?.config,
                eventKey: seatMap.eventKey,
                chartKey: seatMap.chartKey,
                workspaceKey: publicKey,
                usingEventKey: usingEventKey
              });
              setChartRendering(false);
              setChartInitialized(false); // Reset so spinner can show on retry
              // Try to extract error message from various possible locations
              const errorMessage = error?.message
                || error?.error?.message
                || error?.config?.error?.message
                || (typeof error === 'string' ? error : 'Failed to render seat map. Please check if the seat map is properly configured.');
              setError(errorMessage);
            }}
            showLegend={true}
            maxSelectedObjects={seatMap.maxPerUser || seatMap.MaxPerUser || 0}

          />
        )}
      </div>

      {/* Floating Checkout Button and Cart */}
      {getSelectedSeatsCount() > 0 && (
        <>
          {/* Cart Summary - Visible when cartVisible is true */}
          {cartVisible && (
            <div
              style={{
                position: 'fixed',
                bottom: 100,
                right: 24,
                background: '#fff',
                padding: 16,
                borderRadius: 8,
                boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
                minWidth: 280,
                maxWidth: 320,
                zIndex: 1000,
              }}
            >
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
                <Title level={5} style={{ margin: 0 }}>
                  Order Summary
                </Title>
                <Button
                  type="text"
                  icon={<CloseOutlined />}
                  onClick={() => setCartVisible(false)}
                  style={{ padding: 0, width: 24, height: 24 }}
                />
              </div>
              <div style={{ marginBottom: 8 }}>
                <Text>Selected Seats: </Text>
                <Text strong>{getSelectedSeatsCount()}</Text>
              </div>
              <div style={{ marginBottom: 8, maxHeight: 150, overflowY: 'auto', border: '1px solid #f0f0f0', padding: 8, borderRadius: 4, backgroundColor: '#fafafa' }}>
                {getSelectedSeatsData().length > 0 ? (
                  getSelectedSeatsData().map((seat, index) => (
                    <div key={seat.seatKey || index} style={{
                      marginBottom: 4,
                      fontSize: 12,
                      display: 'flex',
                      justifyContent: 'space-between',
                      alignItems: 'center',
                      padding: '4px 8px',
                      backgroundColor: '#fff',
                      borderRadius: 4,
                      border: '1px solid #e8e8e8'
                    }}>
                      <Text strong style={{ fontSize: 13 }}>{seat.label || seat.seatKey}</Text>
                      <Text style={{ marginLeft: 8, color: '#f5222d', fontWeight: 'bold', fontSize: 13 }}>
                        {formatPrice(seat.price || 0)}
                      </Text>
                    </div>
                  ))
                ) : (
                  <Text type="secondary" style={{ fontSize: 12, fontStyle: 'italic' }}>No seats selected</Text>
                )}
              </div>
              <div style={{ marginBottom: 16, borderTop: '1px solid #d9d9d9', paddingTop: 8 }}>
                <Text strong>
                  <DollarOutlined /> Total:{' '}
                </Text>
                <Text strong style={{ color: '#f5222d', fontSize: 18 }}>
                  {formatPrice(getTotalPrice())}
                </Text>
              </div>
              <Button
                type="primary"
                size="large"
                block
                onClick={handleProceedToCheckout}
              >
                Proceed to Checkout
              </Button>
            </div>
          )}

          {/* Floating Action Button - Toggle cart visibility */}
          <FloatButton
            type="primary"
            style={{ right: 24, bottom: 24 }}
            icon={<ShoppingCartOutlined />}
            tooltip={cartVisible ? 'Hide cart' : `Show cart (${getSelectedSeatsCount()} seat(s))`}
            badge={{ count: getSelectedSeatsCount(), overflowCount: 99 }}
            onClick={() => setCartVisible(!cartVisible)}
          />
        </>
      )}

    
    </div>
  );
};

export default CustomerSeatMap;
